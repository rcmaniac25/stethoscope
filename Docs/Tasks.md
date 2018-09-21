# Tasks (and arch. and other things that should be broken out)

## Tasks

PoC:
- ListStorage's Entries will get an exception or completed whenever an entry is added. Need this to work multi-threaded (might require a custom implementation of an Observable instead of ToObservable)
-- Time box: can extensions like Skip/SkipLast be "passed" to parent observable? This way, the "Live" observable can go "I was created but... I see that my first 100 elements will be ignored. I'll simply override that extension so it doesn't run, and source-side, I'll do the skips directly" as it would save time, resources, and a middle-man observable
- (on an independent thread) Analyze logs to try and figure out what some logs are (can I do word counting or whatever the term is to determine similar syntax). Time box, and if no good results are found, then work on log templates to gather contents of logs
- Try to build additional attributes. Such as a common ID or count of some element
- Start trying to correlate additional attributes together. Possibly try to make common processes (fun=a, fun=b, fun=c; 48 times. fun=a, fun=b, fun=d, fun=c; 10 times. fun=a, fun=b, {branch if "args" contains "alt=true"} to d, fun=c). <-- Hard... could be a fun attempt
- Either a txt print out or maybe a GUI that will update with stats
- Iteration through stats (ex. find ID = "steve") and then display results and possibly indicate where things deviated (such as the above "fun=d" example) and other IDs where the same thing happened

What needs to get done. Should make some of them tickets... (some of these may be trains-of-thought)
- "PoC proper" (see details above and from log #102.2)
	- NeverEndingLiveListObservable + method to switch between it and LiveListObservable before subscription (AsInfinite/AsFinite)
	- Initial Qbservable implementation
	- Initial log analysis
	- DB research? ("Database triggers" are the cloeset I've found to being able to implement Qbservable for a DB. If embedable DBs can do it, then I may give it a try)
- To handle larger logs: May have to rewrite IPrinter or change names of PrinterFactory so IOPrinter doesn't "wait" for a specific thread's stream to finish before (starting to) printing the next thread
- Determine program arguments and the config file usage (are they redundent? Complementary? Can I setup everything with program arguments?)
- Add logging (ironic... a log handling system, writing it's own logs). Will be useful when trying to figure out what went wrong with accesing remote logs and doing more advanced work.
- Ensure this works under Mono (as the port to C++ is still a while away, and "this WILL be ported" opinion is always subject to change, though it's currently "it WILL happen")
- Support remote logs
- Proper IQbservable support in RegistryStorage (and maybe additional storage options)
- Look into the status of System.IO.Pipelines to see if more progress has been made (would also need to upgrade to at least .Net Core 2.1 and the .Net Framework equiv. Maybe https://github.com/mgravell/Pipelines.Sockets.Unofficial if nothing new)
- (large) With larger logs, start to flesh out LogRegistry... maybe allow LINQ usage so components of logs can be searched through.
    - [Done?] Needs to support multiple sources (so there may be logs from same time but different log files), and how to query through that
    - How much metadata do we want? (log file names, sources, etc.)
    - Can we parse sub-logs? Like, running an inner process that makes it's own logs (different style, different functions, etc.)... but it outputs to the same log handler. End result: all the logs show up as "myLogHandler: {sublog}"
	- How can we handle log-level structures? As in, some loggers can have a single log line and still write when a function started and when it ended. Knowing something like that would mean that instead of needing to guess at the start and end of a function, or requiring a templeted system, I can just look at a log and say "start and end" and move on.
- Doc and UT work (any areas to catch up on for these?)
- Streaming logs (use a sliding window or similar so we can discard what we don't care about... but what don't we care about?)
    - Are we sure we want to discard? What happens if someone searches for something we don't have? We don't want to read everything every time.
    - Do we want to be "smart" and take stats on how long it takes to read different log files, then use that info to determine how much to discard (in accordance with memory usage and limits)
    - Do we need to abstract anything to make this easier? Like, does the registry now need a "does this log source stream?" and handle it differently or can we have all this fancy sliding-window streaming get done internally, while the user/registry sees `ParseLog`?
- The weird thought: do we want to abstract the processing? For the most part it's: ```log -> log parser -> log registry -> funny query to get logs as desired -> print```. Can we/should we add something between log registry and the query? What would it do? What is "processing"?
- Make decision on GUI work. Read 100DoC log #24.2 as it could be a reason to work on the GUI now, make a framework wrapper, port wrapper to C++, and then work on GUI the way I want to without needing to do a crazy rewrite in Qt or something else. Can also keep syntax modern, instead of dealing with 10-20 years of legacy design.
- Port to C++
    - Pre Req. Start doing branches (master, develop, feature). This allows there to exist a "stable" system that people may be interested in, while develop has all the new logic in it. (RCM) Feature branches will come about if people start contributing... but my gut feeling is that until the "fun" stuff is done, most won't see this as a project to contribute to. So it won't happen yet. Once I start to see some/any interest (that isn't verbally told to me by those that know me already), I need a Code of Conduct, and philosophy written out. "Extreme" but it's too easy to kill projects these days with one bad apple. Stay down to Earth, it's not just about code, and make sure how people can be sure it works, it's maintainable, and it's implemented are useful. (/RCM)
    - v1. Just make a VS project for C++
    - v2. CMake/Bazel files
    - v3. Make .Net wrapper around it (can either CMake/Bazel make C# wrappers?), and get rid of original project so everything is now generated
- Automated building processes in Github so we can move to the real fun work (see "Idea" below) and not need to worry about "we broke something".
- Begin work on self-building function trees (see Ideas.Smart Function Building)
- Initial template integration so debugging is less guessing, and more real (if a structure already exists). Might involve writing a utility to build the templates from code/logs. This would keep the "guessing" to a separate library.
- CLI demo debugging (can I take a random set of logs and be like `select thread 54`, `goto line 30`, `next`, `next`, `skip-over`, `enter`, etc. to debug through)
- Expand debugging so it can handle "timelines". At least initially, to "debug" by thread. So we can iterate through a thread completely for the lifecycle of the log.
- Initial work with real code parsing, very basic syntax parsing (this will not be elegant, but a PoC without going to some outside parser). Abstract so a real parser can be swapped in without much hassle.
- Expand debugging to do selective searches (follow an attribute through a log, multiple threads, etc.) Essentially grep, but with some additional filtering done automatically.
- Initial GUI work. (Need a GUI framework... Qt is popular, but might require a larger rewrite of code then desired, or a wrapper around already existing C++ code)
- RPC/message tracing work.
- Cleanup (make sure code is documented, docs are up to date, parsing works and debugging can occur. Let some people try it and provide feedback)
- Work on user feedback
- Work on a proper CLI (might already have done as debugging work occurred, but I use GUIs and I know people who use CLI only... so how would that like it to work)
- Test runs on different sources of logs (I'm testing on one log set, in a known format. What about some random program online? Can I just find the logs produced by Office, an AV like Norton, or others and parse them? What about OS logs?)
- Event support (logs are a must, but sometimes events are generated instead of a log. We want to support getting events too)
- Can plugins be made for debugging from an IDE? Can Eclipse, Visual Studio, Sublime, VS Code, etc. add a debugger? Can we then automatically parse the code in use, debug some logs as if it's a debugger for following instructions made by the code.
- {that's all for now...}

### Outside Tasks

Tasks that are out of scope, but nice to have

- Contribute to NUnit for docs (bring more emphesis to "you can't use .Net Standard, only .Net Core and .Net Framework for tests") and try to add an automated sanity check that looks at the TargetFrameworkAttribute that assemblies can have, that states what it was built under, in an effort to early exit and say "tests won't be run for that library because it's not a supported library: {link to docs}"

## Architecture

Note: section is fine, but content is... not really an architecture

- One or more libraries that do the work. Possibly allow dynamic loading (do people write custom log formats, or just use JSON/XML/plaintext? Maybe look at existing logs)
- Output is controller by programs, but can be chosen by the user with config/arguments

## Ideas

### Templates

This is to simply the complex task of figuring out how the program runs to make it easier to debug. (RCM) If there's any "hack" to the concept of this program, it's this. But this at least simplifies work without, say, integrating LLVM to parse code. (/RCM)

#### Log Template

Basic idea: user provides "this is how log lines are structured in the content section", that way we know how to parse them without guessing. That way we get a log, parse its structure, and can start to read through it and use its contents

#### Code Template

Basic idea: a graph of function calls. At minimum, just know when one function calls another. At best, know control structures, where logs are called, and handle function pointers.

### Smart Function Building

Logs of any structure will allude to their code's structure. At a naive level (these days, Machine Learning, AI, etc. are the rage... and they're really just fancy names on top of basic concepts): we're counting what function follows another.

So, each time a log line gets parsed, record what function comes before. Eventually, we'll know "this log line is always called after this log line" and will be able to guess it's a subcall (and mark it as such). Eventually, we'll know what functions are called by others based on log lines. If we get the same log lines one after another, we know we're in the same function. If we see different log lines, then back to the original, then we can assume there's more after that function call. If within that data we start to see "X, Y, Z", "X, A, B, Z", "X, Y, Z", and we track when we see function calls, we can say there's a control statement right after the X but before Z. One statement is "Y" and the other is "A, B". If we see "X, Y, Y, Y, Y, Y", "X, Y, Y" we can see a loop. Etc.

How this is done is a different subject, but this can be expanded to other attributes instead of just functions. It could allow starting to trace between threads, between processes, even between servers.

### Remote Data

Loading from remote resources is a must, but some setups are tough. Maybe one idea is to have a graph builder (my hobbies are around games, so I see it as equiv. to Unreal Engine 4's Blueprint Visual Scripting). A system like this could be extended for doing log processing.
