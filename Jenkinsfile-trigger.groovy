#!/bin/groovy
def gitCommitHash = ""

def MONO_BRANCH = "NONE"
def XI_BRANCH = "NONE"
def XM_BRANCH = "NONE"
def XA_BRANCH = "NONE"
def IOS_DEVICE_TYPE = "iPhone-5s"
def IOS_RUNTIME = "iOS-10-3"

def profileSetup ()
{
	def profile = "${env.JENKINS_PROFILE}"
	if (profile == 'master') {
		MONO_BRANCH = 'master'
		XI_BRANCH = 'NONE'
		XM_BRANCH = 'NONE'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = 'iPhone-5s'
		IOS_RUNTIME = "iOS-10-0"
	} else if (profile == '2017-12') {
		MONO_BRANCH = '2017-12'
		XI_BRANCH = 'NONE'
		XM_BRANCH = 'NONE'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = 'iPhone-5s'
		IOS_RUNTIME = "iOS-10-0"
	} else if (profile == '2018-02') {
		MONO_BRANCH = '2018-02'
		XI_BRANCH = 'NONE'
		XM_BRANCH = 'NONE'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = 'iPhone-5s'
		IOS_RUNTIME = "iOS-10-0"
    } else if (profile == '2018-04') {
    	MONO_BRANCH = '2018-04'
    	XI_BRANCH = 'NONE'
    	XM_BRANCH = 'NONE'
    	XA_BRANCH = 'NONE'
    	IOS_DEVICE_TYPE = 'iPhone-5s'
    	IOS_RUNTIME = "iOS-10-0"
    } else if (profile == '2018-06') {
    	MONO_BRANCH = '2018-06'
    	XI_BRANCH = 'NONE'
    	XM_BRANCH = 'NONE'
    	XA_BRANCH = 'NONE'
    	IOS_DEVICE_TYPE = 'iPhone-5s'
    	IOS_RUNTIME = "iOS-10-0"
    } else if (profile == '2018-08') {
    	MONO_BRANCH = '2018-08'
    	XI_BRANCH = 'NONE'
    	XM_BRANCH = 'NONE'
    	XA_BRANCH = 'NONE'
    	IOS_DEVICE_TYPE = 'iPhone-5s'
    	IOS_RUNTIME = "iOS-10-0"
	} else if (profile == 'macios') {
		MONO_BRANCH = 'NONE'
		XI_BRANCH = 'master'
		XM_BRANCH = 'master'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = "iPhone-5s"
		IOS_RUNTIME = "iOS-10-0"
	} else if (profile == 'macios-2018-04') {
		MONO_BRANCH = 'NONE'
		XI_BRANCH = 'mono-2018-04'
		XM_BRANCH = 'mono-2018-04'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = "iPhone-5s"
		IOS_RUNTIME = "iOS-10-0"
	} else if (profile == 'macios-2018-06') {
		MONO_BRANCH = 'NONE'
		XI_BRANCH = 'mono-2018-06'
		XM_BRANCH = 'mono-2018-06'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = "iPhone-5s"
		IOS_RUNTIME = "iOS-10-0"
	} else if (profile == 'macios-2018-08') {
		MONO_BRANCH = 'NONE'
		XI_BRANCH = 'mono-2018-08'
		XM_BRANCH = 'mono-2018-08'
		XA_BRANCH = 'NONE'
		IOS_DEVICE_TYPE = "iPhone-5s"
		IOS_RUNTIME = "iOS-10-0"
	} else {
		error "Unknown Jenkins configuration: $profile"
	}
}

def triggerJob ()
{
    def triggeredBuild = build job: 'web-tests', parameters: [
		string (name: 'MONO_BRANCH', value: MONO_BRANCH),
		string (name: 'XI_BRANCH', value: XI_BRANCH),
		string (name: 'XM_BRANCH', value: XM_BRANCH),
		string (name: 'XA_BRANCH', value: XA_BRANCH),
		string (name: 'IOS_DEVICE_TYPE', value: IOS_DEVICE_TYPE),
		string (name: 'IOS_RUNTIME', value: IOS_RUNTIME),
	], wait: true, propagate: false
	currentBuild.result = triggeredBuild.result
	echo "Build status: ${currentBuild.result}"
	
	def vars = triggeredBuild.getBuildVariables ()
	currentBuild.description = "${triggeredBuild.displayName} - ${triggeredBuild.description}"
	
	rtp nullAction: '1', parserName: 'html', stableText: "<h2>Downstream build: <a href=\"${triggeredBuild.absoluteUrl}\">${triggeredBuild.displayName}</a></h2><p>${triggeredBuild.description}</p>"
	
	// def summaryBadge = manager.createSummary ('info.gif')
	// summaryBadge.appendText ("<h2>Downstream build: <a href=\"${triggeredBuild.absoluteUrl}\">${triggeredBuild.displayName}</a></h2>", false)
	// summaryBadge.appendText ("<p>${triggeredBuild.description}</p>", false)

	// Unset to avoid a NonSerializableException
	def triggeredId = (''+triggeredBuild.id).split('#')[0]
	triggeredBuild = null
	vars = null
	summaryBadge = null
	
	sh "rm -rf artifacts"
	sh "mkdir -p artifacts"
	
	echo "Copying artifacts."
	
	copyArtifacts projectName: 'web-tests', selector: specific (triggeredId), target: 'artifacts', flatten: true, fingerprintArtifacts: true
	
	sh "ls -lR artifacts"
	
	def provisionHtml = 'artifacts/provision-output.html'
	if (fileExists (provisionHtml)) {
		rtp nullAction: '1', parserName: 'html', stableText: "\${FILE:$provisionHtml}"
	}
	
	echo "Publishing html summaries."
	
	def htmlFiles = findFiles (glob: 'artifacts/jenkins-summary-*.html')
	for (file in htmlFiles) {
		rtp nullAction: '1', parserName: 'html', stableText: "\${FILE:$file}"
	}
	
	echo "Done publishing html summaries."
	
	junit keepLongStdio: true, testResults: "artifacts/*.xml"

	echo "Done publishing test results."
}

def slackSend (String color, String message)
{
	slackSend channel: "#martin-jenkins", color: color, message: "${env.JOB_NAME} - ${currentBuild.displayName}: ${message} (<${env.BUILD_URL}|Open>)\n${currentBuild.description}"
}

node ('felix-25-sierra') {
	timestamps {
		stage ('initialize') {
			profileSetup ()
		}
		stage ('build') {
			try {
				triggerJob ()
				if (currentBuild.result == "SUCCESS") {
					slackSend ("good", "Success")
				} else {
					slackSend ("danger", "${currentBuild.result}")
				}
			} catch (exception) {
				currentBuild.result = "FAILURE"
				echo "Caught unhandled exception: $exception"
				slackSend ("danger", "ERROR: $exception")
			}
		}
	}
}
