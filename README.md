# Ye olde link detector

![Docker Image CI](https://github.com/dhedegaard/ye-olde-link-detector/workflows/Docker%20Image%20CI/badge.svg)
[![Maintainability](https://api.codeclimate.com/v1/badges/3133aaa2b17bc268914d/maintainability)](https://codeclimate.com/github/dhedegaard/ye-olde-link-detector/maintainability)

A small discord bot for detecting when duplicate links are being posted.

## I want to use the bot, without setting up anything:

Invite the bot to your server using this URL:

<https://discord.com/api/oauth2/authorize?client_id=754017685888565249&permissions=68608&scope=bot>

The permissions requested are required for the bot to work correctly.

## How to get it up and running.

You need a Discord bot token to run the bot.

Set your bot token in a `TOKEN` env variable.

After that, run:

```sh
$ dotnet run
```
