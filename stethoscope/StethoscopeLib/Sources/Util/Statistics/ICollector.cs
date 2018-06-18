using System;
using System.Collections.Generic;
using System.Text;

namespace Stethoscope.Sources.Util.Statistics
{
    public interface ICollector
    {
    }

    //function call count(finding out how often a function is used)
    //file size(how big of logs should be tested...)
    //what config is used(what kind of configs are applied in context)
    //what attribute is used(what logs are requested from registry)
    //how often specific function areas fail(attribute exception and failure to add attribute)
    //how often failed log entry is "complete"
    //how often log attributes are added to log entries
    //number of "being processed" logs exist
    //number of logs
    //creation criteria for registry creation
    //parser factory extension
    //how often specific path types are used in parsing
    //how often specific path filters are used in parsing
    //unknown(top) xml node types
    //how many log entries are added to a log entry
    //number of times each xml element type is parsed
    //number of unknown xml element types returned by parser
    //number of each parser failure flags
    //number of.Net Standard 2.0 invocations (and inverse)
    //number of key-value elements after cast
    //number of each path type parsed
    //length of parsed paths
    //number of each field type parsed
    //config values used in factories?
    //system stats?
}
