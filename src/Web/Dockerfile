FROM node:22-alpine
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts \
    && npm rebuild esbuild \
    && chown -R node:node /app
COPY --chown=node:node index.html app.js styles.css vite.config.js ./
USER node
EXPOSE 5173
CMD ["npm", "run", "dev"]
