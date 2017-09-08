#!/bin/groovy
properties([
	parameters([
		choice (name: 'QA_USE_MONO_LANE', choices: 'NONE\nmono-2017-06\nmono-2017-08\nmono-martin-new-webstack\nmono-master', description: 'Mono lane'),
		choice (name: 'QA_USE_XI_LANE', choices: 'NONE\nmacios-mac-master', description: 'XI lane'),
		choice (name: 'QA_USE_XM_LANE', choices: 'NONE\nmacios-mac-d15-4\nmacios-mac-master', description: 'XM lane'),
		choice (name: 'QA_USE_XA_LANE', choices: 'NONE\nmonodroid-mavericks-d15-4\nmonodroid-mavericks-master', description: 'XA lane'),
		choice (name: 'IOS_DEVICE_TYPE', choices: 'iPhone-5s', description: ''),
		choice (name: 'IOS_RUNTIME', choices: 'iOS-10-0\niOS-10-3', description: ''),
		string (defaultValue: '', description: '', name: 'EXTRA_JENKINS_ARGUMENTS')
	])
])

def logParsingRuleFile = ""

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

def runShell (String command)
{
    def dir = pwd()
    echo "SHELL ($dir): $command"
    sh command
}

def build (String targets)
{
	dir ('web-tests') {
		runShell ("msbuild Jenkinsfile.targets /t:MultiBuild /p:JenkinsTargets=$targets")
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

def run (String target, String testCategory, String resultOutput, String junitResultOutput, String stdOut, String stdErr)
{
	def iosParams = "IosRuntime=$IOS_RUNTIME,IosDeviceType=$IOS_DEVICE_TYPE"
	def resultParams = "ResultOutput=$resultOutput,JUnitResultOutput=$junitResultOutput"
	def outputParams = "StdOut=$stdOut,StdErr=$stdErr"
	def extraParams = ""
	if (params.EXTRA_JENKINS_ARGUMENTS != '') {
		def extraParamValue = params.EXTRA_JENKINS_ARGUMENTS
		extraParams = ",JenkinsExtraArguments=\"$extraParamValue\""
	}
	runShell ("msbuild Jenkinsfile.targets /t:Run /p:JenkinsTarget=$target,TestCategory=$testCategory,$iosParams,$resultParams,$outputParams$extraParams")
}

def runTests (String target, String category, Boolean unstable = false, Integer timeoutValue = 10)
{
	dir ('web-tests') {
		def outputDir = "out/" + target + "/" + category
		def outputDirAbs = pwd() + "/" + outputDir
		sh "mkdir -p $outputDirAbs"
		def resultOutput = "$outputDirAbs/TestResult-${target}-${category}.xml"
		def junitResultOutput = "$outputDirAbs/JUnitTestResult-${target}-${category}.xml"
        def stdOutLog = "$outputDirAbs/stdout-${target}-${category}.log"
        def stdErrLog = "$outputDirAbs/stderr-${target}-${category}.log"
		Boolean error = false
		try {
			timeout (timeoutValue) {
				run (target, category, resultOutput, junitResultOutput, stdOutLog, stdErrLog)
			}
		} catch (exception) {
			def result = currentBuild.result
			echo "RUN FAILED: $exception $result $unstable"
			if (unstable) {
				currentBuild.result = "UNSTABLE"
				echo "SETTING TO UNSTABLE"
				error = true
			}
		} finally {
			archiveArtifacts artifacts: "$outputDir/*.log", fingerprint: true, allowEmptyArchive: true
			if (!error) {
				junit keepLongStdio: true, testResults: "$outputDir/*.xml"
				archiveArtifacts artifacts: "$outputDir/*.xml", fingerprint: true
			}
		}
	}
}

node ('master') {
    stage ('initialize') {
        // We need to define this on the master node.
        logParsingRuleFile = "${env.WORKSPACE}/../workspace@script/jenkins-log-parser.txt"
    }
}

node ('felix-25-sierra') {
    try {
        timestamps {
            stage ('checkout') {
                dir ('web-tests') {
                    git url: 'git@github.com:xamarin/web-tests.git', branch: 'master'
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
                stage ('console-work') {
                    runTests ('Console', 'Work')
                }
                stage ('console-new') {
                    runTests ('Console', 'New')
                }
                stage ('console-all') {
                    runTests ('Console', 'All')
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
                stage ('android-btls-work') {
                    runTests ('Android-Btls', 'Work', true)
                }
                stage ('android-btls-new') {
                    runTests ('Android-Btls', 'New', true)
                }
                stage ('android-btls-all') {
                    runTests ('Android-Btls', 'All', true, 30)
                }
            }
        }
    } finally {
        stage ('parse-logs') {
            step ([$class: 'LogParserPublisher', parsingRulesPath: "$logParsingRuleFile", useProjectRule: false, failBuildOnError: true]);
        }
    }
}
