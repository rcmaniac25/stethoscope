using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq
{
#if false
    // #1

    internal class BaseQbservable<T> : IQbservable<T>
    {
        public BaseQbservable()
        {
            Provider = new BaseQbservableProvider();
            Expression = Expression.Constant(this);
        }

        public BaseQbservable(BaseQbservableProvider provider, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            if (!typeof(IQbservable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression;
        }

        public Expression Expression { get; private set; }

        public IQbservableProvider Provider { get; private set; }

        public Type ElementType => typeof(T);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return (((BaseQbservableProvider)Provider).Execute<IObservable<T>>(Expression)).Subscribe(observer);
        }
    }

    // #2

    class BaseQbservableProvider : IQbservableProvider
    {
        public IQbservable<T> CreateQuery<T>(Expression expression)
        {
            return new BaseQbservable<T>(this, expression);
        }

        public T Execute<T>(Expression expression)
        {
            var isObservable = (typeof(T).Name == "IObservable`1");

            return (T)BaseQbservableContext.Execute(expression, isObservable);
        }
    }

    // #3

    class BaseQbservableContext
    {
        internal static object Execute(Expression expression, bool isObservable)
        {
            if (!IsQueryOverDataSource(expression))
            {
                throw new InvalidProgramException("No query over the data source was specified.");
            }

            // Find the call to Where() and get the lambda expression predicate.
            var whereFinder = new InnermostWhereFinder();
            var whereExpression = whereFinder.GetInnermostWhere(expression);
            var lambdaExpression = (LambdaExpression)((UnaryExpression)(whereExpression.Arguments[1])).Operand;

            // Send the lambda expression through the partial evaluator.
            lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

            // Get the place name(s) to query the Web service with.
            var lf = new LocationFinder(lambdaExpression.Body);
            var locations = lf.Locations;
            if (locations.Count == 0)
                throw new InvalidQueryException("You must specify at least one place name in your query.");

            // Call the Web service and get the results.
            var places = WebServiceHelper.GetPlacesFromTerraServer(locations);

            // Copy the IEnumerable places to an IQueryable.
            var queryablePlaces = places.AsQueryable<Place>();

            // Copy the expression tree that was passed in, changing only the first 
            // argument of the innermost MethodCallExpression.
            var treeCopier = new ExpressionTreeModifier(queryablePlaces);
            var newExpressionTree = treeCopier.Visit(expression);

            // This step creates an IQueryable that executes by replacing Queryable methods with Enumerable methods. 
            if (isObservable)
                return queryablePlaces.Provider.CreateQuery(newExpressionTree);
            else
                return queryablePlaces.Provider.Execute(newExpressionTree);
        }

        private static bool IsQueryOverDataSource(Expression expression)
        {
            // If expression represents an unqueried IQbservable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression. 
            return (expression is MethodCallExpression);
        }
    }
#endif

    public class QueryableTerraServerData<TData> : IOrderedQueryable<TData>
    {
        #region Constructors
        /// <summary> 
        /// This constructor is called by the client to create the data source. 
        /// </summary> 
        public QueryableTerraServerData()
        {
            Provider = new TerraServerQueryProvider();
            Expression = Expression.Constant(this);
        }

        /// <summary> 
        /// This constructor is called by Provider.CreateQuery(). 
        /// </summary> 
        /// <param name="expression"></param>
        public QueryableTerraServerData(TerraServerQueryProvider provider, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(IQueryable<TData>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }

            Provider = provider ?? throw new ArgumentNullException("provider");
            Expression = expression;
        }
        #endregion

        #region Properties

        public IQueryProvider Provider { get; private set; }
        public Expression Expression { get; private set; }

        public Type ElementType
        {
            get { return typeof(TData); }
        }

        #endregion

        #region Enumerators
        public IEnumerator<TData> GetEnumerator()
        {
            return (Provider.Execute<IEnumerable<TData>>(Expression)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (Provider.Execute<System.Collections.IEnumerable>(Expression)).GetEnumerator();
        }
        #endregion
    }

    public class TerraServerQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(QueryableTerraServerData<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        // Queryable's collection-returning standard query operators call this method. 
        public IQueryable<TResult> CreateQuery<TResult>(Expression expression)
        {
            return new QueryableTerraServerData<TResult>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return TerraServerQueryContext.Execute(expression, false);
        }

        // Queryable's "single value" standard query operators call this method.
        // It is also called from QueryableTerraServerData.GetEnumerator(). 
        public TResult Execute<TResult>(Expression expression)
        {
            var IsEnumerable = (typeof(TResult).Name == "IEnumerable`1");

            return (TResult)TerraServerQueryContext.Execute(expression, IsEnumerable);
        }
    }

    public class Place
    {
        // Properties. 
        public string Name { get; private set; }
        public string State { get; private set; }
        public PlaceType PlaceType { get; private set; }

        // Constructor. 
        internal Place(string name,
                        string state,
                        PlaceType placeType) //XXX: supposed to be from a imported type
        {
            Name = name;
            State = state;
            PlaceType = (PlaceType)placeType;
        }
    }

    public enum PlaceType
    {
        Unknown,
        AirRailStation,
        BayGulf,
        CapePeninsula,
        CityTown,
        HillMountain,
        Island,
        Lake,
        OtherLandFeature,
        OtherWaterFeature,
        ParkBeach,
        PointOfInterest,
        River
    }

    class TerraServerQueryContext
    {
        // Executes the expression tree that is passed to it. 
        internal static object Execute(Expression expression, bool IsEnumerable)
        {
            // The expression must represent a query over the data source. 
            if (!IsQueryOverDataSource(expression))
                throw new InvalidProgramException("No query over the data source was specified.");

            // Find the call to Where() and get the lambda expression predicate.
            var whereFinder = new InnermostWhereFinder();
            var whereExpression = whereFinder.GetInnermostWhere(expression);
            var lambdaExpression = (LambdaExpression)((UnaryExpression)(whereExpression.Arguments[1])).Operand;

            // Send the lambda expression through the partial evaluator.
            lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

            // Get the place name(s) to query the Web service with.
            var lf = new LocationFinder(lambdaExpression.Body);
            var locations = lf.Locations;
            if (locations.Count == 0)
                throw new InvalidQueryException("You must specify at least one place name in your query.");

            // Call the Web service and get the results.
            var places = WebServiceHelper.GetPlacesFromTerraServer(locations);

            // Copy the IEnumerable places to an IQueryable.
            var queryablePlaces = places.AsQueryable<Place>();

            // Copy the expression tree that was passed in, changing only the first 
            // argument of the innermost MethodCallExpression.
            var treeCopier = new ExpressionTreeModifier(queryablePlaces);
            var newExpressionTree = treeCopier.Visit(expression);

            // This step creates an IQueryable that executes by replacing Queryable methods with Enumerable methods. 
            if (IsEnumerable)
                return queryablePlaces.Provider.CreateQuery(newExpressionTree);
            else
                return queryablePlaces.Provider.Execute(newExpressionTree);
        }

        private static bool IsQueryOverDataSource(Expression expression)
        {
            // If expression represents an unqueried IQueryable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression. 
            return (expression is MethodCallExpression);
        }
    }

    internal static class WebServiceHelper
    {
        private static int numResults = 200;
        private static bool mustHaveImage = false;

        internal static Place[] GetPlacesFromTerraServer(List<string> locations)
        {
            // Limit the total number of Web service calls. 
            if (locations.Count > 5)
                throw new InvalidQueryException("This query requires more than five separate calls to the Web service. Please decrease the number of locations in your query.");

            var allPlaces = new List<Place>();

            // For each location, call the Web service method to get data. 
            foreach (string location in locations)
            {
                var places = CallGetPlaceListMethod(location);
                allPlaces.AddRange(places);
            }

            return allPlaces.ToArray();
        }

#if false
        private static Place[] CallGetPlaceListMethod(string location)
        {

            var client = new TerraServiceSoapClient();
            PlaceFacts[] placeFacts = null;

            try
            {
                // Call the Web service method "GetPlaceList".
                placeFacts = client.GetPlaceList(location, numResults, mustHaveImage);

                // If there are exactly 'numResults' results, they are probably truncated. 
                if (placeFacts.Length == numResults)
                    throw new Exception("The results have been truncated by the Web service and would not be complete. Please try a different query.");

                // Create Place objects from the PlaceFacts objects returned by the Web service.
                var places = new Place[placeFacts.Length];
                for (int i = 0; i < placeFacts.Length; i++)
                {
                    places[i] = new Place(
                        placeFacts[i].Place.City,
                        placeFacts[i].Place.State,
                        placeFacts[i].PlaceTypeId);
                }

                // Close the WCF client.
                client.Close();

                return places;
            }
            catch (TimeoutException timeoutException)
            {
                client.Abort();
                throw;
            }
            catch (System.ServiceModel.CommunicationException communicationException)
            {
                client.Abort();
                throw;
            }
        }
#else
        private static readonly Place[] masterList = new Place[]
        {
            new Place("Central Park", "New York", PlaceType.ParkBeach),
            new Place("Grand Central Station", "New York", PlaceType.AirRailStation),
            new Place("Penn Station", "New York", PlaceType.AirRailStation),
            new Place("John F. Kennedy Airport", "New York", PlaceType.AirRailStation),
            new Place("La Guardia Airport", "New York", PlaceType.AirRailStation),
            new Place("New York City", "New York", PlaceType.CityTown),
            new Place("Manhattan", "New York", PlaceType.Island),
            new Place("Staten Island", "New York", PlaceType.Island),
            new Place("Battery Park", "New York", PlaceType.ParkBeach),
            new Place("Hudson River", "New York", PlaceType.River),
            new Place("East River", "New York", PlaceType.River),
            new Place("Penn Station", "New Jersey", PlaceType.AirRailStation),
            new Place("Newark Airport", "New Jersey", PlaceType.AirRailStation)
        };
        private static Dictionary<string, int[]> wordToMaster = new Dictionary<string, int[]>();

        private static void BuildWordToMap()
        {
            //XXX: not optimized for speed. That's why we do this once and never again

            var words = new HashSet<string>();
            foreach (var place in masterList)
            {
                words.Add(place.Name.ToLower());
                words.Add(place.State.ToLower());

                if (place.Name.IndexOf(' ') > 0)
                {
                    var parts = place.Name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLower());
                    words.UnionWith(parts);
                }
                if (place.State.IndexOf(' ') > 0)
                {
                    var parts = place.State.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLower());
                    words.UnionWith(parts);
                }
            }

            foreach (var word in words)
            {
                var indices = new HashSet<int>();
                for (int i = 0; i < masterList.Length; i++)
                {
                    var place = masterList[i];
                    if (place.Name.ToLower().Contains(word))
                    {
                        indices.Add(i);
                    }
                    if (place.State.ToLower().Contains(word))
                    {
                        indices.Add(i);
                    }
                }

                wordToMaster.Add(word, indices.ToArray());
            }
        }

        private static Place[] CallGetPlaceListMethod(string location)
        {
            if (wordToMaster.Count == 0)
            {
                BuildWordToMap();
            }

            var lowerCaseLocation = location.ToLower();

            var wordMatches = new List<string>();
            foreach (var keys in wordToMaster.Keys)
            {
                if (keys.Contains(lowerCaseLocation)) //XXX: if specific matching work is needed. Think, regex. Do it here
                {
                    wordMatches.Add(keys);
                }
            }

            if (wordMatches.Count > 0)
            {
                var placeSet = new HashSet<int>();
                foreach (var word in wordMatches)
                {
                    placeSet.UnionWith(wordToMaster[word]);
                }

                var places = new List<Place>();
                foreach (int index in placeSet)
                {
                    places.Add(masterList[index]);
                }

                return places.ToArray();
            }
            
            throw new InvalidQueryException("Location not found");
        }
#endif
        }

    internal class InnermostWhereFinder : ExpressionVisitor
    {
        private MethodCallExpression innermostWhereExpression;

        public MethodCallExpression GetInnermostWhere(Expression expression)
        {
            Visit(expression);
            return innermostWhereExpression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name == "Where")
                innermostWhereExpression = expression;

            Visit(expression.Arguments[0]);

            return expression;
        }
    }

    internal class LocationFinder : ExpressionVisitor
    {
        private Expression expression;
        private List<string> locations;

        public LocationFinder(Expression exp)
        {
            this.expression = exp;
        }

        public List<string> Locations
        {
            get
            {
                if (locations == null)
                {
                    locations = new List<string>();
                    this.Visit(this.expression);
                }
                return this.locations;
            }
        }

        protected override Expression VisitBinary(BinaryExpression be)
        {
            if (be.NodeType == ExpressionType.Equal)
            {
                if (ExpressionTreeHelpers.IsMemberEqualsValueExpression(be, typeof(Place), "Name"))
                {
                    locations.Add(ExpressionTreeHelpers.GetValueFromEqualsExpression(be, typeof(Place), "Name"));
                    return be;
                }
                else if (ExpressionTreeHelpers.IsMemberEqualsValueExpression(be, typeof(Place), "State"))
                {
                    locations.Add(ExpressionTreeHelpers.GetValueFromEqualsExpression(be, typeof(Place), "State"));
                    return be;
                }
                else
                    return base.VisitBinary(be);
            }
            else
                return base.VisitBinary(be);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(String) && m.Method.Name == "StartsWith")
            {
                if (ExpressionTreeHelpers.IsSpecificMemberExpression(m.Object, typeof(Place), "Name") ||
                ExpressionTreeHelpers.IsSpecificMemberExpression(m.Object, typeof(Place), "State"))
                {
                    locations.Add(ExpressionTreeHelpers.GetValueFromExpression(m.Arguments[0]));
                    return m;
                }

            }
            else if (m.Method.Name == "Contains")
            {
                Expression valuesExpression = null;

                if (m.Method.DeclaringType == typeof(Enumerable))
                {
                    if (ExpressionTreeHelpers.IsSpecificMemberExpression(m.Arguments[1], typeof(Place), "Name") ||
                    ExpressionTreeHelpers.IsSpecificMemberExpression(m.Arguments[1], typeof(Place), "State"))
                    {
                        valuesExpression = m.Arguments[0];
                    }
                }
                else if (m.Method.DeclaringType == typeof(List<string>))
                {
                    if (ExpressionTreeHelpers.IsSpecificMemberExpression(m.Arguments[0], typeof(Place), "Name") ||
                    ExpressionTreeHelpers.IsSpecificMemberExpression(m.Arguments[0], typeof(Place), "State"))
                    {
                        valuesExpression = m.Object;
                    }
                }

                if (valuesExpression == null || valuesExpression.NodeType != ExpressionType.Constant)
                    throw new Exception("Could not find the location values.");

                ConstantExpression ce = (ConstantExpression)valuesExpression;

                IEnumerable<string> placeStrings = (IEnumerable<string>)ce.Value;
                // Add each string in the collection to the list of locations to obtain data about. 
                foreach (string place in placeStrings)
                    locations.Add(place);

                return m;
            }

            return base.VisitMethodCall(m);
        }
    }

    internal class ExpressionTreeModifier : ExpressionVisitor
    {
        private IQueryable<Place> queryablePlaces;

        internal ExpressionTreeModifier(IQueryable<Place> places)
        {
            this.queryablePlaces = places;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            // Replace the constant QueryableTerraServerData arg with the queryable Place collection. 
            if (c.Type == typeof(QueryableTerraServerData<Place>))
                return Expression.Constant(this.queryablePlaces);
            else
                return c;
        }
    }

    public static class Evaluator
    {
        /// <summary> 
        /// Performs evaluation & replacement of independent sub-trees 
        /// </summary> 
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }

        /// <summary> 
        /// Performs evaluation & replacement of independent sub-trees 
        /// </summary> 
        /// <param name="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
        public static Expression PartialEval(Expression expression)
        {
            return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        /// <summary> 
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down) 
        /// </summary> 
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return this.Visit(exp);
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == null)
                {
                    return null;
                }
                if (this.candidates.Contains(exp))
                {
                    return this.Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                var lambda = Expression.Lambda(e);
                var fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }

        /// <summary> 
        /// Performs bottom-up analysis to determine which nodes can possibly 
        /// be part of an evaluated sub-tree. 
        /// </summary> 
        class Nominator : ExpressionVisitor
        {
            Func<Expression, bool> fnCanBeEvaluated;
            HashSet<Expression> candidates;
            bool cannotBeEvaluated;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                this.candidates = new HashSet<Expression>();
                this.Visit(expression);
                return this.candidates;
            }

            public override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                    this.cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!this.cannotBeEvaluated)
                    {
                        if (this.fnCanBeEvaluated(expression))
                        {
                            this.candidates.Add(expression);
                        }
                        else
                        {
                            this.cannotBeEvaluated = true;
                        }
                    }
                    this.cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }
    }

    internal static class TypeSystem
    {
        internal static Type GetElementType(Type seqType)
        {
            var ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;

            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());

            if (seqType.IsGenericType)
            {
                foreach (var arg in seqType.GetGenericArguments())
                {
                    var ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }

            var ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (var iface in ifaces)
                {
                    var ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }

            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }

            return null;
        }
    }

    internal class ExpressionTreeHelpers
    {
        internal static bool IsMemberEqualsValueExpression(Expression exp, Type declaringType, string memberName)
        {
            if (exp.NodeType != ExpressionType.Equal)
                return false;

            var be = (BinaryExpression)exp;

            // Assert. 
            if (ExpressionTreeHelpers.IsSpecificMemberExpression(be.Left, declaringType, memberName) &&
                ExpressionTreeHelpers.IsSpecificMemberExpression(be.Right, declaringType, memberName))
                throw new Exception("Cannot have 'member' == 'member' in an expression!");

            return (ExpressionTreeHelpers.IsSpecificMemberExpression(be.Left, declaringType, memberName) ||
                ExpressionTreeHelpers.IsSpecificMemberExpression(be.Right, declaringType, memberName));
        }

        internal static bool IsSpecificMemberExpression(Expression exp, Type declaringType, string memberName)
        {
            return ((exp is MemberExpression) &&
                (((MemberExpression)exp).Member.DeclaringType == declaringType) &&
                (((MemberExpression)exp).Member.Name == memberName));
        }

        internal static string GetValueFromEqualsExpression(BinaryExpression be, Type memberDeclaringType, string memberName)
        {
            if (be.NodeType != ExpressionType.Equal)
                throw new Exception("There is a bug in this program.");

            if (be.Left.NodeType == ExpressionType.MemberAccess)
            {
                var me = (MemberExpression)be.Left;

                if (me.Member.DeclaringType == memberDeclaringType && me.Member.Name == memberName)
                {
                    return GetValueFromExpression(be.Right);
                }
            }
            else if (be.Right.NodeType == ExpressionType.MemberAccess)
            {
                var me = (MemberExpression)be.Right;

                if (me.Member.DeclaringType == memberDeclaringType && me.Member.Name == memberName)
                {
                    return GetValueFromExpression(be.Left);
                }
            }

            // We should have returned by now. 
            throw new Exception("There is a bug in this program.");
        }

        internal static string GetValueFromExpression(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
                return (string)(((ConstantExpression)expression).Value);
            else
                throw new InvalidQueryException(
                    String.Format("The expression type {0} is not supported to obtain a value.", expression.NodeType));
        }
    }

    class InvalidQueryException : System.Exception
    {
        private string message;

        public InvalidQueryException(string message)
        {
            this.message = message + " ";
        }

        public override string Message
        {
            get
            {
                return "The client query is invalid: " + message;
            }
        }
    }
}
