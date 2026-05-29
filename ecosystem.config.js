module.exports = {
  apps: [{
    name: "bsp-web",
    script: "node_modules/next/dist/bin/next",
    args: "start",
    cwd: __dirname,
    exec_interpreter: "node",
    env: {
      NODE_ENV: "production",
      PORT: 3000
    }
  }]
};
