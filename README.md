CFT
===

.net config file transformations tool

## Usage

``` powershell
    # transform using a configuration name
    cft transform /path=. /configuration=Production

    # cascade transformation are supported (will apply Production, and then Production-Amazon if found)
    cft transform /path=. /configuration=Production-Amazon

    # dry run, doesn't touch files and throws an error if a file would be changed
    cft transform /path=. /configuration=Production /dry

    # get configuration name from different sources (priority is: parameter, file, env, default)
    cft transform /path=.  /configurationFile=configurationName.tmp /configurationEnv=CONFIGURATIONNAME /configurationDefault=Local

    # Run without params to see help
    cft
```

This tool will find all *.default.config files recursively and apply corresponding transformations when found.

## Tokens

These tokens will be replaced at the end of transformation:

 - ```$configurationName$```: the configuration name used to transform
 - ```$env:VARIABLE_NAME$```: an environment variable

## Nuget

Install as a nuget package: https://nuget.org/packages/cft/

To run this tool on prebuild event or on post-deploy, you'll need to find /packages/cft.x.x.x.x/ path.
To make that easy you can install [NugetToolsHelper](https://nuget.org/packages/NugetToolHelper/) package