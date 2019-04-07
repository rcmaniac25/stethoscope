# Extra Configs

LogConfig contains all config values to use when parsing.

Now, for a bit of added control, multiple LogConfigs can be created or one master one can be cloned and modifed, and then each used as needed.

This allows for configs for different logs to be created if needed.

But not every config can be thought of or represented in a common form. As such, LogConfig.ExtraConfigs exists.

## Available Extra Configs

### Stethoscope-defined IPrinter(s)

- "printMode" = <Mode> | <Format> (See [Print Mode](#print-mode) for more info)

### Tracker program

- "printToFile" = File path to write the results of Tracker's execution to a file.

## Details

### Print Mode

The idea behind a print mode is similar to a string format (C#'s composite format or C/C++'s printf-style), but for defining info about how to print a log.

Custom modes can be defined with a "format" while pre-defined modes can be used, simply referred to as "mode".

- "mode":
    - General = @!"Problem printing log. Timestamp=^{Timestamp}, Message=^{Message}"[{Timestamp}] -- {Message}^{LogSource|, LogSource="{}"}^{ThreadID|, ThreadID="{}"}...^{Context|, Context="{}"}
	    - Every attribute is printed
    - FunctionOnly = @{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
    - FirstFunctionOnly = @~{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
	- DifferentFunctionOnly = @${Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"

- "format"
    - format = `@[<conditional>...][<modifier>...]<part>[<part>...]` | `<mode>`
    - `<part>` = `<raw>` | `<attribute>`
    - `<raw>` = (any charecter except `+ - ^ $ ~ ! { }`. Special chars need to be duplicated to print)
    - `<attribute>` = `[<conditional>]<attribute reference>[<modifier>]` (note: order matters for evaluation purposes. So a condition will always be evaluated before a modifier, while which modifier gets tested first will depend on where it is in the format)
    - `<conditional>`
		- `^` (only print if attribute exists. Only applies to attributes)
        - `$` (print only when the value changes from the last log entry. Not applicable per-log. So a,a,a,b,a,b,b,a -> a,b,a,b,a. Only applies to attributes)
		- `~` (print only if the value hasn't been printed before. Only applies to attributes)
        - `+` (only print if a valid log)
        - `-` (only print if an invalid log)
    - `<modifier>`
		- `!"<format>"` (if an error occurs with this log, print the following error message. Limitations: must be contained within quotes, double quotes needs to be escaped `\"`)
		- //FUTURE-TODO (something to do if it fails the conditional check)
		- //FUTURE-TODO (failure handler: on error, don't print attribute; on error, don't print log; on error, print attribute error handler; on error, log-format error handler; on error, throw exception/error; on error, report error [Default])
    - `<attribute reference>` - `{<attribute name>[<attribute format>]}`
    - `<attribute name>` - (any of the LogAttribute enum value names)
    - `<attribute format>` - `|<raw>` (inside `<raw>`, any "{}" (quotes not included) will be replaced with the value from the attribute)
