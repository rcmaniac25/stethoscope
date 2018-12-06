# Extra Configs

LogConfig contains all config values to use when parsing.

Now, for a bit of added control, multiple LogConfigs can be created or one master one can be cloned and modifed, and then each used as needed.

This allows for configs for different logs to be created if needed.

But not every config can be thought of or represented in a common form. As such, LogConfig.ExtraConfigs exists.

## Available Extra Configs

### Stethoscope-defined IPrinter(s)

- "printMode" = <TODO> (how to print... see #230.2 for thoughts)

### Tracker program

- "printToFile" = File path to write the results of Tracker's execution to a file.
