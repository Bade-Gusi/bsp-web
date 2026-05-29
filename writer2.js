
const fs = require("fs");
const p = require("path");
const bd = "D:/学习/ddddddddddfxxg/bsp-web";

function w(relPath, content) {
  const fp = p.join(bd, relPath);
  fs.mkdirSync(p.dirname(fp), { recursive: true });
  fs.writeFileSync(fp, content, "utf-8");
  console.log("OK: " + relPath);
}

// File 1: dashboard
w("src/app/dashboard/page.tsx", "
" + String.raw`
placeholder dashboard content
`);
