"use strict";

module.exports = function(grunt) {

	grunt.loadTasks('./tasks');
    grunt.loadNpmTasks('grunt-msbuild');
    grunt.loadNpmTasks('grunt-release');

 	grunt.initConfig({
 		msbuild: {
	        options: {
	            stdout: true,
	            version: 4.0,
	            buildParameters: {
	                WarningLevel: 2
	            },
	            verbosity: 'quiet'
        	},
        	default: {
        		src: ['src/VsDebugger.sln'],
                options: {
                    targets: ['Clean', 'Build'],
                    projectConfiguration: 'Release'
                }
        	}
        },
        vs_debugger: {
        	options: {
        		solutionName: 'AirPlans.sln',
        		processName: 'w3wp'
        	}
        },
        release: {
            options: { commitMessage: 'NPM Release v<%= version %>' }
        }
 	});

 	grunt.registerTask('default', ['msbuild'])
}