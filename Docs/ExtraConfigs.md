# Extra Configs

LogConfig contains all config values to use when parsing.

Now, for a bit of added control, multiple LogConfigs can be created or one master one can be cloned and modifed, and then each used as needed.

This allows for configs for different logs to be created if needed.

But not every config can be thought of or represented in a common form. As such, LogConfig.ExtraConfigs exists.

## Available Extra Configs

### Stethoscope-defined IPrinter(s)

- "printMode" = <Mode> | <Format> (See #PrintMode for more info)

### Tracker program

- "printToFile" = File path to write the results of Tracker's execution to a file.

## Details

### Print Mode

The idea behind a print mode is similar to a string format (C#'s composite format or C/C++'s printf-style), but for defining info about how to print a log.

Custom modes can be defined with a "format" while pre-defined modes can be used, simply referred to as "mode".

- "mode":
-- General = TODO
-- FunctionOnly = @{Function} ```Error condition TODO```
-- FirstFunctionOnly = @{Function}$ ```Error condition TODO```

- "format"
-- format = @<part>[<part>...] | <mode>
-- <part> = <raw> | <attribute>
-- <raw> = "any charecter except ^,$,{,}. Special chars need to be duplicated to print
-- <attribute> = [<conditional>]{attribute name}[<modifier>]
-- <conditional> - ^ (only print if it exists)
-- <modifier>
--- $ (print only when the value changes from the last log. So a,a,a,b,a,b,b,a -> a,b,a,b,a)
--- !"<format>" (if an error occurs with this log, print the following error message. Limitations: must be contained within quotes)
-- {attribute name} - (any of the LogAttribute enum value names, surrounded by {})
