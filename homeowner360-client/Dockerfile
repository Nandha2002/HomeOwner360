# Build stage
FROM node:20-alpine AS build

WORKDIR /app

COPY homeowner360-client/package*.json ./

RUN npm ci

COPY homeowner360-client/ .

# NEXT_PUBLIC_* variables are embedded into the client bundle
ARG NEXT_PUBLIC_API_URL=http://localhost:8080/api
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

RUN npm run build


# Runtime stage
FROM node:20-alpine AS runner

WORKDIR /app

ENV NODE_ENV=production

COPY --from=build /app/public ./public
COPY --from=build /app/.next ./.next
COPY --from=build /app/package.json ./package.json
COPY --from=build /app/node_modules ./node_modules

EXPOSE 3000

CMD ["npm", "start"]