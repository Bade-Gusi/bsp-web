/** @type {import('next').NextConfig} */
const nextConfig = {
  images: { unoptimized: true },
  basePath: '',
  async rewrites() {
    return [
      { source: '/api/:path*', destination: 'http://localhost:5001/api/:path*' },
      { source: '/hubs/:path*', destination: 'http://localhost:5001/hubs/:path*' },
      { source: '/callhub/:path*', destination: 'http://localhost:5001/callhub/:path*' },
    ]
  },
}

module.exports = nextConfig
