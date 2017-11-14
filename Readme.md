# AutoClient

A .NET Core 2.0 cli application for testing the vautointerview.azurewebsites.net API endpoint.

## Build Dependencies

* .net core 2.0 SDK
* VS 2017 15.4.x (C# 7.1 language support)
* Nuget Access

## Usage Examples

* Example 1 - Basic Execution

        dotnet AutoClient.dll -l Trace

* Example 2 - Override Endpoint

        dotnet AutoClient.dll -l Trace -u http://myvautointerview2.elasticbeanstalk.com

## Output

        2017/11/14 16:11:51.938|DEBUG|Vehicle Detail added for Vehicle Id '134974136' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:52.865|DEBUG|Vehicle Detail added for Vehicle Id '341796290' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:53.935|DEBUG|Vehicle Detail added for Vehicle Id '300929599' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:54.013|DEBUG|Vehicle Detail added for Vehicle Id '352287762' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:54.914|DEBUG|Vehicle Detail added for Vehicle Id '295821685' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:54.914|DEBUG|Vehicle Detail added for Vehicle Id '683571731' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:55.090|DEBUG|Vehicle Detail added for Vehicle Id '931534939' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:56.983|DEBUG|Vehicle Detail added for Vehicle Id '1305156430' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:11:57.994|DEBUG|Vehicle Detail added for Vehicle Id '864694115' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg'. |AutoClient.VAutoClient|
        2017/11/14 16:12:02.081|DEBUG|Dealer Detail added for Dealer Id '1793461740' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg' |AutoClient.VAutoClient|
        2017/11/14 16:12:04.164|DEBUG|Dealer Detail added for Dealer Id '974860003' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg' |AutoClient.VAutoClient|
        2017/11/14 16:12:06.232|DEBUG|Dealer Detail added for Dealer Id '1105007578' from endpoint 'http://vautointerview.azurewebsites.net', using Dataset Id '1CJSU6Qr1Qg' |AutoClient.VAutoClient|

        AutoClient Results:
        {
        "success": true,
        "message": "Congratulations.",
        "totalMilliseconds": 15841
        }
