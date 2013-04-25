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

## Nuget

Install as a nuget package: https://nuget.org/packages/cft/

cft.ps1 will be added to your project, in order to distribute it with your app Include it on the project and Copy to Output.

This script can be added to your prebuild event and/or run on your post-deploy tasks. It will find cft.exe on /packages folder or locally and allows you to obtain the configuration name from different sources.

``` powershell
    # transform config files in the project using Production configuration name
    .\cft.ps1 Production

    # transform config files in all projects
    .\cft.ps1 Production -Path ~\..

    # transform config files using configuration name obtained from a file
    .\cft.ps1 -ConfigurationNameFile configurationName.tmp

    # transform config files using configuration name obtained from an env variable
    .\cft.ps1 -ConfigurationNameEnv CONFIGURATIONNAME

    # transform config files with fallback thru different sources (file, env, default)
    .\cft.ps1 -ConfigurationNameFile configurationName.tmp -ConfigurationNameEnv CONFIGURATIONNAME -ConfigurationNameDefault LocalW
```