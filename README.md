# Stethoscope - a debugger for when code debugging isn't an option

**Note**: anything prefixed with "{rcm}" is me, the author, as this document is a work in progress. So those are not "sterile, emotionless, opinionless" voice of a document of info.

## About

Debugging applications is an integral part of software development.

In many, if not most, areas of development, a normal debugger works. Attach to a process, a website, C, Java, Go, TypeScript, etc. If it can produce a bug, it will, and a debugger will help you get out of it.

But what if you can't? What if you're high-availability, multi-service/microservice, written in JS, C++, Java, Go, crosses three availability zone service used by tens of thousands to millions of people is so extensive that your options boil down to:

- Manually tracing through each service individually, looking at logs, events, or some monitoring dashboard
- Kabana, Splunk, or some other aggregate system that lists out every word match, but not why your user/customer can't send their cat gif
- Database queries for events or records of specific events
- Unit tests, integration tests, load tests, etc. to check that certain errors/warnings, logs, etc. don't occur and that the end-to-end test works as expected
- Rewrites of services so tests, events, logs, etc. can tell you if it's working.
- Hope...

Enter **Stethoscope**, the tool that will (eventually) allow a combination of logs, events, and more sources of data to let you debug through an application from multiple systems and backends, to get a full picture of what happened or what's happening.

## Why

{rcm}
I encounter issues fairly frequently that require searching across multiple machines and DBs to get a full picture of what went wrong or what's happening. Sometimes I can look at a single server and know what's going on, other times I spend multiple days just triaging if an ticket is a bug or not. It's very frustrating and I think many have just accepted that it's how to do it...

...I also needed a project for 100 Days of Code. :D
{/rcm}

## How

{rcm}
I don't know. I have a very generic idea, but someone will probably tell me there's a better way or a poor way. So I'm wingging it. Design can be done at a high level, but code is iterative. A final version will never appear first. So I'm gonna try a couple things, see what sticks, do some research when I have an idea, and hopefully eventually get to something that can work quickly and efficently. And doing iteration will result in something, hopefully, that is what I envision and does what I want. Others may get involved, and they'll shape it too.

So... high level design? At least for a first go, PoC, etc.
{/rcm}

- Log Parsing: Initially, to prevent needing to do a massive machine learning and text analysis, templates are needed for logs. What is each field in a log line? What is success and what is failure? For a function, what are the log lines that it will produce? What other functions does it call? Etc. {rcm} For testing/project creation, this will be manually written functions. But... this is tedious and it would probably be useful to create a generator to produce these templates based off code.{/rcm}
- Processing: Choose some unique ID from the log and have the system "follow" it
- Output: Inital implementation will just print out text, along the lines of a stack trace. So it will list each function it touches, handling of `if`/`for`/`while` etc. looping will be indicated.
- Bonus: if I can parse values from logs to partially fill in function arguments, it will probably make things nicer to use.
- Required features: remote access ({rcm}As per the "About", if you have many log sources, gathering them all together doesn't always help figure out what happend. Your code base is cloned to your computer or a server you're logged into, but your test/production system isn't on the same system. So to load logs, it needs to access remote machines. Possibly untar/ungzip files to use them.{/rcm})

{rcm}That's enough for now. Once a clearer picture of code progress is determined, this will be updated.{/rcm}

## Disclamer

Disclamers exist for the sake of preventing the wrong idea about a project:

1. This isn't a disassembly/reversing tool. It's expected logs, code, etc. exist. It exists because it's assumed that a project can't be adapted/rewritten for a different logging system, or similar.
2. {rcm}This came up from my experience at work, and they are aware I'm working on it. I'm purposly developing it without touching work content. But... I will be "using it" at work, so there will be templates and logs that I can't publish. So suddenly doing some massive change will probably come simply from going "this is a pain, what do I need to get it to work?" I'm dotting my i's and checking that I don't do anything that would prevent me from working on it... but it is a project born out of a desire to have a better way to deal with bugs at work.{/rcm}
