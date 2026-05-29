/** @type {import('next').NextConfig} */
const nextConfig = {
  images: { unoptimized: true },
  basePath: '',
  async rewrites() {
    return [
      { source: '/api/:path*', destination: 'http://localhost:5000/api/:path*' },
    ]
  },
}

module.exports = nextConfig
