FROM node:22-bookworm-slim AS build
WORKDIR /app
ENV ONNXRUNTIME_NODE_INSTALL=skip
COPY package*.json ./
RUN npm install --global "$(node -p 'require("./package.json").packageManager')" \
    && npm ci --ignore-scripts

COPY . .
RUN npm run build

FROM node:22-bookworm-slim AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000

COPY --from=build /app/.output ./.output

EXPOSE 3000
CMD ["node", ".output/server/index.mjs"]
