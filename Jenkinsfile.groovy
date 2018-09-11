#!/bin/groovy
properties([
	parameters([
		choice (name: 'MONO_BRANCH', choices: 'NONE\nCURRENT\nSPECIFIC\n2017-12\n2018-02\n2018-04\n2018-06\n2018-08\nmaster', description: 'Mono branch'),
		choice (name: 'XI_BRANCH', choices: 'NONE\nCURRENT\nSPECIFIC\nmaster\nd15-6\nmono-2018-06\nmono-2018-08', description: 'XI branch'),
		choice (name: 'XM_BRANCH', choices: 'NONE\nCURRENT\nSPECIFIC\nmaster\nd15-6\nmono-2018-06\nmono-2018-08', description: 'XM branch'),
		choice (name: 'XA_BRANCH', choices: 'NONE\nCURRENT\nSPECIFIC\nmaster\nd15-6\nmono-2018-06\nmono-2018-08', description: 'XA branch'),
		choice (name: 'IOS_DEVICE_TYPE', choices: 'iPhone-5s', description: ''),
		choice (name: 'IOS_RUNTIME', choices: 'iOS-10-0\niOS-10-3', description: ''),
		string (name: 'MONO_COMMIT', defaultValue: '', description: 'Use specific Mono commit'),
		string (name: 'XI_COMMIT', defaultValue: '', description: 'Use specific XI commit'),
		string (name: 'XM_COMMIT', defaultValue: '', description: 'Use specific XM commit'),
		string (name: 'XA_COMMIT', defaultValue: '', description: 'Use specific Android commit'),
		string (name: 'EXTRA_JENKINS_ARGUMENTS', defaultValue: '', description: ''),
		booleanParam (name: 'SPECIFIC_COMMIT', defaultValue: false, description: 'Use specific commit')
	])
])

//
// Specifying branches and commits:
// * NONE - skip this product
// * CURRENT - keep what's currently installed on the bot
// * SPECIFIC - use specific commit (which needs to be set in the corresponding '_COMMIT' parameter)
//
// If you put anything into the '_COMMIT' parameter without using "SPECIFIC", then it will be appended
// to the branch name using the @{commit} syntax - for instance "master@{yesterday}"
//
// By default, the AutoProvisionTool will go back up to 25 commits from the one specified until it
// finds one that has a package set as github status.  Set the 'SPECIFIC_COMMIT' parameter to disable this.
//

def logParsingRuleFile = ""
def gitCommitHash = ""

def getBranchAndCommit (String name, String branch, String commit)
{
	if (branch == 'NONE' || branch == '' || branch == null) {
		return null
	}
	if (branch == 'SPECIFIC') {
		if (commit == '') {
			error "Must set $name commit."
			return null
		}
		return commit
	}
	if (commit != '') {
		return "$branch@{$commit}"
	}
	return branch
}

def provision ()
{
	final String OUTPUT_DIRECTORY = 'artifacts'

	def args = [ ]
	def monoBranch = getBranchAndCommit ('Mono', params.MONO_BRANCH, params.MONO_COMMIT)
	if (monoBranch != null) {
		args << "--mono=$monoBranch"
	}
	
	def xiBranch = getBranchAndCommit ('XI', params.XI_BRANCH, params.XI_COMMIT)
	if (xiBranch != null) {
		args << "--xi=$xiBranch"
	}
	
	def xmBranch = getBranchAndCommit ('XM', params.XM_BRANCH, params.XM_COMMIT)
	if (xmBranch != null) {
		args << "--xm=$xmBranch"
	}
	
	def xaBranch = getBranchAndCommit ('XA', params.XA_BRANCH, params.XA_COMMIT)
	if (xaBranch != null) {
		args << "--xa=$xaBranch"
	}
	
	if (params.SPECIFIC_COMMIT) {
		args << "--specific"
	}
	
	def buildPath = new URI (env.BUILD_URL).getPath()
	env.WEB_TESTS_BUILD_PATH = buildPath
	
	def summaryFile = "summary.txt"
	def provisionOutput = "provision-output.txt"
	def provisionHtml = "provision-output.html"
	args << "--summary=$OUTPUT_DIRECTORY/$summaryFile"
	args << "--out=$OUTPUT_DIRECTORY/$provisionOutput"
	args << "--html=$OUTPUT_DIRECTORY/$provisionHtml"
	args << "--jenkins-job=$buildPath"
	def argList = args.join (" ")
	dir ('web-tests/Tools/AutoProvisionTool') {
		try {
			runShell ("mkdir -p $OUTPUT_DIRECTORY")
			runShell ("nuget restore AutoProvisionTool.sln")
			runShell ("msbuild /verbosity:minimal AutoProvisionTool.sln")
			withCredentials ([string(credentialsId: 'mono-webtests-github-token', variable: 'JENKINS_OAUTH_TOKEN')]) {
				runShell ("mono --debug ./bin/Debug/AutoProvisionTool.exe $argList provision")
			}
		} finally {
			dir (OUTPUT_DIRECTORY) {
				archiveArtifacts artifacts: "provision-output.*", fingerprint: true, allowEmptyArchive: true
				rtp nullAction: '1', parserName: 'html', stableText: "\${FILE:$provisionHtml}"
				def summary = readFile summaryFile
				echo "Setting build summary: $summary"
				currentBuild.description = summary
				env.WEB_TESTS_PROVISION_SUMMARY = summary
			}
		}
	}
}

def enableMono ()
{
	return params.MONO_BRANCH != 'NONE' && params.MONO_BRANCH != ''
}

def enableXI ()
{
	return params.XI_BRANCH != 'NONE' && params.XI_BRANCH != ''
}

def enableXM ()
{
	return params.XM_BRANCH != 'NONE' && params.XM_BRANCH != ''
}

def enableXA ()
{
	return params.XA_BRANCH != 'NONE' && params.XA_BRANCH != ''
}

def runShell (String command)
{
    sh command
}

def build (String targets)
{
	dir ('web-tests') {
		runShell ("msbuild /verbosity:minimal Jenkinsfile.targets /t:MultiBuild /p:JenkinsTargets=$targets")
	}
}

def buildAll ()
{
	def targets = [ ]
	if (enableMono ()) {
		targets << "Console"
		targets << "Console-AppleTls"
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

def run (String target, String testCategory, String outputDir, String resultOutput, String junitResultOutput, String stdOut, String jenkinsHtml)
{
    final String localExtraJenkinsArguments = ""

	def buildPath = new URI (env.BUILD_URL).getPath()
	def iosParams = "IosRuntime=$IOS_RUNTIME,IosDeviceType=$IOS_DEVICE_TYPE"
	def resultParams = "ResultOutput=$resultOutput,JUnitResultOutput=$junitResultOutput"
	def outputParams = "StdOut=$stdOut,JenkinsHtml=$jenkinsHtml,JenkinsJob=$buildPath"
	def extraParams = ""
	if (params.EXTRA_JENKINS_ARGUMENTS != '') {
		def extraParamValue = params.EXTRA_JENKINS_ARGUMENTS
		extraParams = ",ExtraJenkinsArguments=\"$extraParamValue\""
    } else {
        extraParams = ",ExtraJenkinsArguments=\"$localExtraJenkinsArguments\""
    }
	withEnv (['MONO_ENV_OPTIONS=--debug']) {
		runShell ("msbuild /verbosity:minimal Jenkinsfile.targets /t:Run /p:JenkinsTarget=$target,TestCategory=$testCategory,OutputDir=$outputDir,$iosParams,$resultParams,$outputParams$extraParams")
	}
}

def androidInstall (String target, Integer timeoutValue = 15)
{
	final String OUTPUT_DIRECTORY = 'artifacts'

	dir ('web-tests') {
        def outputLog = "install-${target}.log"
		def buildPath = new URI (env.BUILD_URL).getPath()
		def extraParams = ""
		if (params.EXTRA_JENKINS_ARGUMENTS != '') {
			def extraParamValue = params.EXTRA_JENKINS_ARGUMENTS
			extraParams = ",ExtraJenkinsArguments=\"$extraParamValue\""
		}
		try {
			timeout (timeoutValue) {
				withEnv (['MONO_ENV_OPTIONS=--debug']) {
					runShell ("msbuild /verbosity:minimal Jenkinsfile.targets /t:AndroidInstall /p:JenkinsTarget=$target,StdOut=$outputLog$extraParams")
				}
			}
		} catch (exception) {
			def result = currentBuild.result
			echo "RUN FAILED: $exception $result"
			currentBuild.result = "UNSTABLE"
			echo "SETTING TO UNSTABLE"
			error = true
		} finally {
			dir (OUTPUT_DIRECTORY) {
				if (fileExists (outputLog))
					archiveArtifacts artifacts: outputLog, fingerprint: true, allowEmptyArchive: true
			}
		}
	}
}

def runTests (String target, String category, Boolean unstable = false, Integer timeoutValue = 15)
{
	final String OUTPUT_DIRECTORY = 'artifacts'

	dir ('web-tests') {
		def outputDir = target + "/" + category
		def resultOutput = "$outputDir/TestResult-${target}-${category}.xml"
		def junitResultOutput = "$outputDir/JUnitTestResult-${target}-${category}.xml"
        def outputLog = "$outputDir/output-${target}-${category}.log"
		def jenkinsHtmlLog = "$outputDir/jenkins-summary-${target}-${category}.html"
		Boolean error = false
		try {
			timeout (timeoutValue) {
				run (target, category, OUTPUT_DIRECTORY, resultOutput, junitResultOutput, outputLog, jenkinsHtmlLog)
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
			dir (OUTPUT_DIRECTORY) {
				if (fileExists (outputLog))
					archiveArtifacts artifacts: outputLog, fingerprint: true, allowEmptyArchive: true
				if (fileExists (jenkinsHtmlLog)) {
					archiveArtifacts artifacts: jenkinsHtmlLog, fingerprint: true, allowEmptyArchive: true
					rtp nullAction: '1', parserName: 'html', stableText: "\${FILE:$jenkinsHtmlLog}"
				}
				if (!error) {
					junit keepLongStdio: true, testResults: "$outputDir/*.xml"
					archiveArtifacts artifacts: "$outputDir/*.xml", fingerprint: true
				}
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
					gitCommitHash = sh (script: "git log -n 1 --pretty=format:'%h'", returnStdout: true)
					currentBuild.displayName = "#$currentBuild.number:$gitCommitHash"
					env.WEB_TESTS_COMMIT = gitCommitHash
					env.WEB_TESTS_BUILD = currentBuild.displayName
                }
            }
            stage ('provision') {
				provision ()
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
				stage ('android-install') {
					androidInstall ('Android-Btls', 15)
				}
                stage ('android-btls-work') {
                    runTests ('Android-Btls', 'Work', true)
                }
                stage ('android-btls-new') {
                    runTests ('Android-Btls', 'New', true)
                }
                stage ('android-btls-all') {
                    runTests ('Android-Btls', 'All', true, 60)
                }
            }
        }
    } finally {
        stage ('parse-logs') {
            step ([$class: 'LogParserPublisher', parsingRulesPath: "$logParsingRuleFile", useProjectRule: false, failBuildOnError: true]);
        }
    }
}
