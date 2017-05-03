#!/bin/groovy
properties([
	parameters([
		choice (name: 'QA_USE_MONO_LANE', choices: 'NONE\nmono-2017-04\nmono-2017-02\nmono-master', description: 'Mono lane'),
		choice (name: 'QA_USE_XI_LANE', choices: 'NONE\nmacios-mac-d15-2\nmacios-mac-master', description: 'XI lane'),
		choice (name: 'QA_USE_XM_LANE', choices: 'NONE\nmacios-mac-d15-2\nmacios-mac-master', description: 'XM lane'),
		choice (name: 'QA_USE_XA_LANE', choices: 'NONE\nmonodroid-mavericks-master', description: 'XA lane'),
		choice (name: 'IOS_DEVICE_TYPE', choices: 'iPhone-5s', description: ''),
		choice (name: 'IOS_RUNTIME', choices: 'iOS-10-0\niOS-10-3', description: '')
	])
])

def provision (String product, String lane)
{
	dir ('QA/Automation/XQA') {
		if ("$lane" != 'NONE') {
			sh "./build.sh --target XQASetup --category=Install$product -Verbose -- -UseLane=$lane"
		} else {
			echo "Skipping $product."
		}
	}
}

def provisionMono ()
{
	provision ('Mono', params.QA_USE_MONO_LANE)
}

def provisionXI ()
{
	provision ('XI', params.QA_USE_XI_LANE)
}

def provisionXM ()
{
	provision ('XM', params.QA_USE_XM_LANE)
}

def provisionXA ()
{
	provision ('XA', params.QA_USE_XA_LANE)
}

def enableMono ()
{
	return params.QA_USE_MONO_LANE != 'NONE'
}

def enableXI ()
{
	return params.QA_USE_XI_LANE != 'NONE'
}

def enableXM ()
{
	return params.QA_USE_XM_LANE != 'NONE'
}

def enableXA ()
{
	return params.QA_USE_XA_LANE != 'NONE'
}

def build (String targets)
{
	dir ('web-tests') {
		sh "msbuild Jenkinsfile.targets /t:MultiBuild /p:JenkinsTargets=$targets"
	}
}

def buildAll ()
{
	def targets = [ ]
	if (enableMono ()) {
		targets << "Console"
		targets << "Console-AppleTls"
		targets << "Console-Legacy"
	}
	if (enableXI ()) {
		targets << "IOS"
	}
	if (enableXM ()) {
		targets << "Mac"
	}
	if (enableXA ()) {
		targets << "Android-Btls"
	}
	
	if (targets.size() == 0) {
		echo "No configurations enabled!"
		currentBuild.result = "NOT_BUILT"
		return
	}
	
	def targetList = targets.join (":")
	build (targetList)
}

def run (String target, String testCategory, String resultOutput, String junitResultOutput)
{
	iosParams = "IosRuntime=$IOS_RUNTIME,IosDeviceType=$IOS_DEVICE_TYPE"
	resultParams = "ResultOutput=$resultOutput,JUnitResultOutput=$junitResultOutput"
	sh "msbuild Jenkinsfile.targets /t:Run /p:JenkinsTarget=$target,TestCategory=$testCategory,$iosParams,$resultParams"
}

def runTests (String target, String category, int timeout = 10)
{
	dir ('web-tests') {
		def outputDir = "out/" + target + "/" + category
		def outputDirAbs = pwd() + "/" + outputDir
		sh "mkdir -p $outputDirAbs"
		def resultOutput = "$outputDirAbs/TestResult-${target}-${category}.xml"
		def junitResultOutput = "$outputDirAbs/JUnitTestResult-${target}-${category}.xml"
		try {
			timeout (timeout) {
				run (target, category, resultOutput, junitResultOutput)
			}
		} catch (error) {
			def result = currentBuild.result
			echo "RUN FAILED: $error $result"
		} finally {
			junit keepLongStdio: true, testResults: "$outputDir/*.xml"
			archiveArtifacts artifacts: "$outputDir/*.xml", fingerprint: true
		}
	}
}

node ('jenkins-mac-1') {
	timestamps {
		stage ('checkout') {
			dir ('web-tests') {
				git url: 'git@github.com:xamarin/web-tests.git', branch: 'jenkins-pipeline'
				sh 'git clean -xffd'
			}
			dir ('QA') {
				git url: 'git@github.com:xamarin/QualityAssurance.git'
			}
		}
		stage ('provision') {
			provisionMono ()
			provisionXI ()
			provisionXM ()
			provisionXA ()
		}
		stage ('build') {
			buildAll ()
		}
		if (enableMono ()) {
			stage ('console-martin') {
				runTests ('Console', 'Martin')
			}
			stage ('console-work') {
				runTests ('Console', 'Work')
			}
			stage ('console-new') {
				runTests ('Console', 'New')
			}
			stage ('console-all') {
				runTests ('Console', 'All')
			}
			stage ('console-appletls-martin') {
				runTests ('Console-AppleTls', 'Martin')
			}
			stage ('console-appletls-work') {
				runTests ('Console-AppleTls', 'Work')
			}
			stage ('console-appletls-new') {
				runTests ('Console-AppleTls', 'New')
			}
			stage ('console-appletls-all') {
				runTests ('Console-AppleTls', 'All')
			}
			stage ('console-legacy-martin') {
				runTests ('Console-Legacy', 'Martin')
			}
			stage ('console-legacy-work') {
				runTests ('Console-Legacy', 'Work')
			}
			stage ('console-legacy-new') {
				runTests ('Console-Legacy', 'New')
			}
			stage ('console-legacy-all') {
				runTests ('Console-Legacy', 'All')
			}
		}
		if (enableXI ()) {
			stage ('ios-martin') {
				runTests ('IOS', 'Martin')
			}
			stage ('ios-work') {
				runTests ('IOS', 'Work')
			}
			stage ('ios-new') {
				runTests ('IOS', 'New')
			}
			stage ('ios-all') {
				runTests ('IOS', 'All')
			}
		}
		if (enableXM ()) {
			stage ('mac-martin') {
				runTests ('Mac', 'Martin')
			}
			stage ('mac-work') {
				runTests ('Mac', 'Work')
			}
			stage ('mac-new') {
				runTests ('Mac', 'New')
			}
			stage ('mac-all') {
				runTests ('Mac', 'All')
			}
		}
		if (enableXA ()) {
			stage ('android-btls-martin') {
				runTests ('Android-Btls', 'Martin')
			}
			stage ('android-btls-work') {
				runTests ('Android-Btls', 'Work')
			}
			stage ('android-btls-new') {
				runTests ('Android-Btls', 'New')
			}
			stage ('android-btls-all') {
				runTests ('Android-Btls', 'All', 30)
			}
		}
	}
}
