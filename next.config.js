/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',
  basePath: '/bsp-web',
  images: { unoptimized: true },
  trailingSlash: true,
}

module.exports = nextConfig
