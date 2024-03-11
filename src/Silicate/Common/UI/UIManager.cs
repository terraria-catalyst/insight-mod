using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace TeamCatalyst.Silicate.Common.UI;

public sealed class UIManager : ModSystem
{
    // Keeps track of the last game time update, which isn't provided for rendering.
    private static GameTime gameTime;

    public static UserInterface State { get; private set; }

    public override void Load()
    {
        base.Load();

        State = new UserInterface();
        State.SetState(new UITileDisplay());
    }

    public override void Unload()
    {
        base.Unload();

        State.CurrentState?.Deactivate();
        State.SetState(null);
        State = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        base.UpdateUI(gameTime);

        State.Update(gameTime);

        UIManager.gameTime = gameTime;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        base.ModifyInterfaceLayers(layers);

        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");

        if (index == -1)
        {
            return;
        }

        layers.Insert(index + 1, new LegacyGameInterfaceLayer("Silicate:TileDisplay", static () =>
        {
            State.Draw(Main.spriteBatch, gameTime);

            return true;
        }, InterfaceScaleType.UI));
    }
}