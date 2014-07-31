module.exports = function(grunt){
	grunt.registerTask('vs_debugger', 'Attach Visual Studio debugger to process', function() {
		var options = this.options(),
			done = this.async(),
			pathToExe = '../src/bin/Release/VsDebugger.exe';

		console.log(require('path').resolve(__dirname, pathToExe));
		if (!options.solutionName)
			grunt.fail.fatal('solutionName option is missing');
		if (!options.processName)
			grunt.fail.fatal('processName option is missing');
		
		var attacher = require('child_process').spawn(require('path').resolve(__dirname, pathToExe), [options.solutionName, options.processName], {
			 stdio: [ 'ignore', process.stdout, process.stderr ]
		});

		attacher.on('close', done);
	});
}