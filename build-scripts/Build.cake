using System.Text.RegularExpressions;

#addin nuget:?package=RestSharp&version=106.1.0  
#tool "nuget:?package=JetBrains.dotCover.CommandLineTools"
#tool "NUnit.ConsoleRunner"
#tool "NUnit.Extension.VSProjectLoader"
#load "./variables/variables.cake" 

var target = Argument("target", "build");
var _config = "Debug";
var _solutionDirectory = "../Source";
var solutionFile = _solutionDirectory + "/Dressed.sln";
string outputPath = null;

 

Task("GetCoverage")
  .Does(() =>
{  
	Information("coverageFunctionUrl: "+ coverageFunctionBaseUrl);
	var client = new RestSharp.RestClient(coverageFunctionBaseUrl);
	var request = new RestSharp.RestRequest(getCoverageFunctionEndpoint, RestSharp.Method.GET);    
		
	var response = client.Execute(request); 
	Information(response.Content);
	
	Information("Hello GetCoverage!");
});


Task("StoreCoverage")
  .Does(() =>
{
  var newCoverage = 68; 
   
  var client = new RestSharp.RestClient(coverageFunctionBaseUrl);
  var request = new RestSharp.RestRequest(setCoverageFunctionEndpoint, RestSharp.Method.POST);   
  request.RequestFormat = RestSharp.DataFormat.Json;
  request.AddBody(new { percentage = newCoverage }); // uses JsonSerializer 
  var response = client.Execute(request); 
  Information(response.Content); 
  Information("Hello YO!");
});


Task("Build").Description("Builds the solution").Does(() =>
{  
		Information("Building solution: " + solutionFile);
		var msbuildSettings = new MSBuildSettings() 
		{   Verbosity = Verbosity.Minimal
			, Configuration=_config
			, MaxCpuCount = 0 // this will use all processors and build in parallel
			, ArgumentCustomization = args=>args.Append("/property:WarningLevel=0") 
		};
  
		if(outputPath != null)
		{
			msbuildSettings.Properties.Add("OutputPath", new[]{outputPath});
		}

		Information("Configuration is " + msbuildSettings.Configuration);

		MSBuild(solutionFile, msbuildSettings); 
});

Task("TestOnly").Description("Runs tests against already compiled solution").Does(() =>
{
	TestOnly();
});

public void TestOnly()
{ 
	var testDlls =   GetFiles(@"..\Source\Dressed.Business.Test\bin\" + _config + @"\Dressed.Business.Test.dll");
	Information("Found " + testDlls.Count() + " test dlls");

	
	foreach(FilePath testDll in testDlls)
	{
		Information("Test Dll: " + testDll.FullPath);
	}
 
	var nUnitSettings = new NUnit3Settings {
		StopOnError = true
	};
	 
	var dotCoverAnalyseSettings =  new DotCoverAnalyseSettings()
		.WithFilter("+:Dressed.Business*") 
		.WithFilter("-:Dressed.*Test");
	dotCoverAnalyseSettings.ReportType = DotCoverReportType.HTML;

	var resultsFile = "results/codeCoverage/dot-cover-results.html"; 
	DotCoverAnalyse(tool => {
			tool.NUnit3(testDlls,nUnitSettings);
		},
		new FilePath(resultsFile),
		dotCoverAnalyseSettings
	); 

	var currentCoverage = 1; // this will be pulled out and read from the code coverage from the last dev build
	var test = new System.Text.RegularExpressions.Regex(@"\[\[.Total.,(\d*),");
	StreamReader file = new StreamReader(resultsFile);
 
	var line = "";
	while ((  line  = file.ReadLine()) != null)
	{  
		var m = test.Match(line);
		int matchCount = 0;
		if (m.Success) 
		{ 
			Information("Match "+ m.Groups[1].Value);  
			var totalCoverage = Int32.Parse(m.Groups[1].Value);
			if ( totalCoverage < currentCoverage)
			{
				throw new Exception(string.Format("error - the total code coverage percentage of this build {0}% is below the develop branch percentage of {1}%", totalCoverage, currentCoverage) );
			}
			else
			{
				Information("The build code coverage of "+ totalCoverage+" which is better than the current code coverage of " + currentCoverage);

			}
			break;
		}
	} 
}




RunTarget(target);