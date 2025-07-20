module.exports = {
  presets: [
    'module:@react-native/babel-preset',
    '@babel/preset-typescript'
  ],
  plugins: [
    [
      'module-resolver',
      {
        root: ['./src'],
        extensions: ['.ios.js', '.android.js', '.js', '.ts', '.tsx', '.json'],
        alias: {
          '@components': './src/components',
          '@screens': './src/screens',
          '@services': './src/services'
        }
      }
    ]
  ]
};
