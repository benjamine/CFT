CFT
===

.net config file transformations tool

## Usage

``` powershell
    # create empty files if they don't exist
    cft BaseDirectory/

    # transform using a configuration name
    cft BaseDirectory/ Production

    # cascade transformation are supported (will apply Production, and then Production-Amazon if found)
    cft BaseDirectory/ Production-Amazon

    # use a different destination
    cft BaseDirectory/ Production DestinationDirectory/
```

This tool will find all *.default.config files recursively and apply corresponding transformations when found.

## Tokens

These tokens will be replaced at the end of transformation:

 - ```$configurationName$```: the configuration name used to transform
 - ```$env:VARIABLE_NAME$```: an environment variable
