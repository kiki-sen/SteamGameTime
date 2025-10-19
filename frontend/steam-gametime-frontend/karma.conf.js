// Karma configuration file, see link for more information
// https://karma-runner.github.io/1.0/config/configuration-file.html

module.exports = function (config) {
  const isCI = process.env.CI || process.env.GITHUB_ACTIONS;
  
  config.set({
    basePath: '',
    frameworks: ['jasmine'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage')
    ],
    client: {
      jasmine: {
        // you can add configuration options for Jasmine here
        // the possible options are listed at https://jasmine.github.io/api/edge/Configuration.html
        // for example, you can disable the random execution order
        // random: false
      },
      clearContext: false // leave Jasmine Spec Runner output visible in browser
    },
    jasmineHtmlReporter: {
      suppressAll: true // removes the duplicated traces
    },
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage/steam-gametime-frontend'),
      subdir: '.',
      reporters: [
        { type: 'html' },
        { type: 'text-summary' }
      ]
    },
    reporters: ['progress', 'kjhtml'],
    browsers: isCI ? ['ChromeHeadless'] : ['Brave'],
    customLaunchers: {
      Brave: {
        base: 'Chrome',
        flags: [
          '--no-sandbox',
          '--disable-setuid-sandbox',
          '--disable-gpu',
          '--remote-debugging-port=9222'
        ]
      },
      BraveHeadless: {
        base: 'Chrome',
        flags: [
          '--headless',
          '--no-sandbox',
          '--disable-setuid-sandbox',
          '--disable-gpu',
          '--remote-debugging-port=9222'
        ]
      }
    },
    restartOnFileChange: true
  });
  
  // Set up Brave path for local development
  if (!isCI) {
    const os = require('os');
    const path = require('path');
    const fs = require('fs');
    
    let bravePath;
    if (os.platform() === 'win32') {
      const possiblePaths = [
        path.join(os.homedir(), 'AppData', 'Local', 'BraveSoftware', 'Brave-Browser', 'Application', 'brave.exe'),
        'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
        'C:\\Program Files (x86)\\BraveSoftware\\Brave-Browser\\Application\\brave.exe'
      ];
      
      for (const p of possiblePaths) {
        try {
          fs.accessSync(p);
          bravePath = p;
          break;
        } catch (e) {
          // Continue to next path
        }
      }
    }
    
    if (bravePath) {
      process.env.CHROME_BIN = bravePath;
    }
  }
};