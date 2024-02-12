const { stringify } = require('querystring');
const { token, id } = require('./config.json');
const { Client, Events, GatewayIntentBits, SlashCommandBuilder } = require('discord.js');
const { get } = require('http');

const bot = new Client({
    intents: [
        GatewayIntentBits.Guilds
    ]
});

bot.once(Events.ClientReady, c =>{
    console.log(` ${c.user.tag} online`);

    const players = new SlashCommandBuilder()
        .setName('players')
        .setDescription('Activity players EggWars');

    bot.application.commands.create(players, "id") 
});

bot.on(Events.InteractionCreate, async interaction => {
    if (!interaction.isChatInputCommand()) return;
    if (interaction.commandName === "players"){
        
    }
})


bot.login(token);