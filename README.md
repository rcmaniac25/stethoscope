# Stethoscope - a debugger for when code debugging isn't an option

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

Enter Stethoscope, the tool that will (eventually) allow a combination of logs, events, and more sources of data to let you debug through an application from multiple systems and backends, to get a full picture of what happened or what's happening.

## Why

I encounter issues fairly frequently that require searching across multiple machines and DBs to get a full picture of what went wrong or what's happening. Sometimes I can look at a single server and know what's going on, other times I spend multiple days just triaging if an ticket is a bug or not. It's very frustrating and I think many have just accepted that it's how to do it...
...I also needed a project for 100 Days of Code. :D
