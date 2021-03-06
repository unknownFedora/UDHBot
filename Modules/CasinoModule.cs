﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot
{
    public class CasinoModule : ModuleBase
    {
        private readonly CasinoService _casinoService;


        public CasinoModule(CasinoService casinoService)
        {
            _casinoService = casinoService;
        }

        [Command("slot"), Summary("Play the slot machine. Syntax : !slot amount")]
        [Alias("slotmachine")]
        private async Task SlotMachine(int amount)
        {
            if (Context.Channel.Id != Settings.GetCasinoChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            IUser user = Context.User;

            var reply = _casinoService.PlaySlotMachine(user, amount);
            if (reply.imagePath != null)
                await Context.Channel.SendFileAsync(reply.imagePath, reply.reply);
            else
                await ReplyAsync(reply.reply);
        }

        [Command("udc"), Summary("Get the amount of UDC. Syntax : !udc")]
        [Alias("coins", "coin")]
        private async Task Coins()
        {
            if (Context.Channel.Id != Settings.GetCasinoChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            IUser user = Context.User;

            await ReplyAsync($"{user.Mention} you have **{_casinoService.GetUserUdc(user.Id)}**UDC");
        }
        
        [Command("jackpot"), Summary("Get the slot machine jackpot. Syntax : !jackpot")]
        private async Task Jackpot()
        {
            if (Context.Channel.Id != Settings.GetCasinoChannel())
            {
                await Task.Delay(1000);
                await Context.Message.DeleteAsync();
                return;
            }

            await ReplyAsync(_casinoService.SlotCashPool());
        }
    }
}