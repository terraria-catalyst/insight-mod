using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace TeamCatalyst.Silicate.Common.UI;

/// <summary>
///     Displays information about the tile currently being hovered at the player's cursor.
/// </summary>
public sealed class UITileDisplay : UIState
{
    // Splits PascalCase and numbers to look more presentable.
    private static readonly Regex Pattern = new("(\\B[A-Z0-9])", RegexOptions.Compiled);

    private Player Player => Main.LocalPlayer;

    public UIImage Panel { get; private set; }

    public UIImage Bar { get; private set; }
    
    public UIImageFramed TileImage { get; private set; }

    public UIText TileName { get; private set; }

    public UIText ModName { get; private set; }

    public float Opacity { get; private set; }

    public override void OnInitialize()
    {
        base.OnInitialize();

        // TODO: Central UIElement for the full area of the state.
        // Will also avoid progress bar being cut out from the overflow.

        Panel = new UIImage(Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Panel"))
        {
            OverflowHidden = true,
            ScaleToFit = true,
            HAlign = 0.5f,
            Top = StyleDimension.FromPixels(24f),
            Height = StyleDimension.FromPixels(48f),
            OverrideSamplerState = SamplerState.PointClamp
        };

        Append(Panel);

        TileImage = new UIImageFramed(TextureAssets.Tile[0], new Rectangle(9 * 18, 3 * 18, 16, 16))
        {
            MarginLeft = 8f,
            VAlign = 0.5f,
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(TileImage);

        TileName = new UIText(string.Empty, 0.8f)
        {
            MarginTop = 8f,
            MarginLeft = 32f,
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(TileName);

        ModName = new UIText(string.Empty, 0.6f)
        {
            MarginBottom = 8f,
            MarginLeft = 32f,
            VAlign = 1f,
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(ModName);

        Bar = new UIImage(Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Progress"))
        {
            ScaleToFit = true,
            VAlign = 1f,
            Left = StyleDimension.FromPixels(-2f),
            Height = StyleDimension.FromPixels(2f),
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(Bar);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateColors();

        UpdatePanel();
        UpdateProgress();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        // Panel borders are drawn separately to ensure a smoother resizing. Avoids texture stretching.
        spriteBatch.Draw(
            Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Panel_Left").Value,
            Panel.GetDimensions().Position() - new Vector2(4f, 0f),
            Color.White * Opacity
        );

        spriteBatch.Draw(
            Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Panel_Right").Value,
            Panel.GetDimensions().Position() + new Vector2(Panel.Width.Pixels, 0f),
            Color.White * Opacity
        );
    }

    private void UpdateColors()
    {
        Panel.Color = Color.White * Opacity;

        TileImage.Color = Color.White * Opacity;

        TileName.TextColor = Color.White * Opacity;
        ModName.TextColor = new Color(88, 88, 173) * Opacity;

        Bar.Color = Color.Lerp(Color.Black, Color.White, Bar.Width.Pixels / (Panel.Width.Pixels + 2f)) * Opacity;
    }

    private void UpdatePanel()
    {
        Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);

        if (tile.HasTile && TileID.Search.TryGetName(tile.TileType, out string? name))
        {
            DynamicSpriteFont? font = FontAssets.MouseText.Value;

            string tileName = Pattern.Replace(name, " $1");
            Vector2 tileNameSize = font.MeasureString(tileName);

            TileName.SetText(tileName);

            Mod? mod = TileLoader.GetTile(tile.TileType)?.Mod;
            string? modName = mod == null ? "Terraria" : mod.DisplayName;
            Vector2 modNameSize = font.MeasureString(modName);

            ModName.SetText(modName);

            Asset<Texture2D>? texture = TextureAssets.Tile[tile.TileType];
            Rectangle frame = Main.tileFrameImportant[tile.TileType] ? new Rectangle(9 * 18, 3 * 18, 16, 16) : new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);

            TileImage.SetImage(texture, frame);

            float desiredWidth = MathF.Max(tileNameSize.X, modNameSize.X);
            float width = MathHelper.Lerp(Panel.Width.Pixels, desiredWidth, 0.2f) + frame.Width;

            Panel.Width.Set(MathF.Ceiling(width), 0f);

            Recalculate();

            Opacity = MathHelper.Lerp(Opacity, 1f, 0.2f);
        }
        else
        {
            Asset<Texture2D>? texture = Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Unknown");

            TileImage.SetImage(texture, texture.Frame());

            TileName.SetText(string.Empty);
            ModName.SetText(string.Empty);

            Opacity = MathHelper.Lerp(Opacity, 0f, 0.2f);
        }
    }

    private void UpdateProgress()
    {
        float progress = 0f;
        int index = Player.hitTile.TryFinding(Player.tileTargetX, Player.tileTargetY, 1);

        if (index != -1)
        {
            HitTile.HitTileObject? data = Player.hitTile.data[index];

            if (data == null)
            {
                return;
            }

            // TODO: Find a way to make the progress bar fill up after the tile is mined.
            progress = (Panel.Width.Pixels + 2f) * data.damage / 100f;
        }

        Bar.Width.Set(MathHelper.Lerp(Bar.Width.Pixels, progress, 0.2f), 0f);
    }
}