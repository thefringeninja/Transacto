FROM node:alpine

RUN apk add --update-cache \
    inotify-tools && \
    rm -rf /var/cache/apk/*

WORKDIR /opt/somecompany/

COPY ./public ./public

COPY package.json yarn.lock ./

RUN chown -R node:node .

USER node

RUN yarn

COPY . .

USER root

RUN chown -R node:node ./src

USER node

EXPOSE 3000 3001

CMD ["yarn", "start"]
