import { assertEquals } from "https://deno.land/std@0.68.0/testing/asserts.ts";
import { processMessage } from "../process-message.ts";
import type { Message } from "../deps.ts";
import { ChannelTypes } from "../deps.ts";
import { clearData, getGuildData } from "../data.ts";

const fakeMessage: Message = {
  id: "test-id",
  timestamp: new Date("2020-01-01T12:34:56Z").getTime(),
  author: {
    id: "user-id",
    username: "fake-username",
    bot: false,
    avatar: null,
    discriminator: "test",
  },
  attachments: [],
  // @ts-expect-error - Ignore unneeded fields.
  channel: {
    guildID: "guild-id",
    id: "channel-id",
    lastPinTimestamp: undefined,
    mention: "",
    nsfw: true,
    rateLimitPerUser: undefined,
    type: ChannelTypes.GUILD_TEXT,
    userLimit: undefined,
  },
  channelID: "channel-id",
  content: "test-content",
  editedTimestamp: undefined,
  embeds: [],
  // @ts-expect-error - ignore unneeded fields.
  guild: {},
  guildID: "guild-id",
  // @ts-expect-error - ignore unneeded fields.
  member: {},
  mentionChannels: undefined,
  mentionRoles: [],
  // @ts-expect-error - ignore unneeded fields.
  mentions: {},
  mentionsEveryone: false,
  messageReference: undefined,
  pinned: false,
  tts: false,
  type: 0,
  webhookID: undefined,
};

Deno.test("Should return an empty array of the message is from a bot", () => {
  assertEquals(
    processMessage({
      ...fakeMessage,
      author: {
        ...fakeMessage.author,
        bot: true,
      },
    }),
    []
  );
});

Deno.test(
  "Should return an empty array if there are no URLs in the message",
  () => {
    // assertEquals(
    processMessage({
      ...fakeMessage,
    });
    //   []
    // );
  }
);

Deno.test(
  "Should return an empty array, if there's an URL but it's the first time we see it and the guild.",
  () => {
    clearData();

    assertEquals(
      processMessage({
        ...fakeMessage,
        content: "Some url: http://example.com",
      }),
      []
    );
    assertEquals(getGuildData("guild-id"), {
      seenMessageIds: [],
      urls: {
        "http://example.com": [
          {
            messageid: fakeMessage.id,
            timestamp: new Date(fakeMessage.timestamp).toISOString(),
            userid: "user-id",
            username: "fake-username",
          },
        ],
      },
    });
  }
);

Deno.test(
  "Should return an element in the array, when an existing URL is encountered.",
  () => {
    clearData();
    const oneYearAgo = new Date(fakeMessage.timestamp);
    oneYearAgo.setFullYear(oneYearAgo.getFullYear() - 1);
    getGuildData(fakeMessage.guildID).urls = {
      "http://example.com": [
        {
          messageid: "old-fake-message-id",
          timestamp: oneYearAgo.toISOString(),
          userid: "user-id",
          username: "fake-username",
        },
      ],
    };

    const result = processMessage({
      ...fakeMessage,
      content: "Some url: http://example.com",
    });

    assertEquals(result, [
      {
        firstTimePosted: {
          messageid: "old-fake-message-id",
          timestamp: new Date("2019-01-01T12:34:56.000Z").toISOString(),
          userid: "user-id",
          username: "fake-username",
        },
        postCount: 1,
        url: "http://example.com",
        userid: "user-id",
      },
    ]);
    assertEquals(getGuildData("guild-id"), {
      seenMessageIds: [],
      urls: {
        "http://example.com": [
          // The original message.
          {
            messageid: "old-fake-message-id",
            timestamp: oneYearAgo.toISOString(),
            userid: "user-id",
            username: "fake-username",
          },
          // The new double posted message.
          {
            messageid: fakeMessage.id,
            timestamp: new Date(fakeMessage.timestamp).toISOString(),
            userid: "user-id",
            username: "fake-username",
          },
        ],
      },
    });
  }
);
