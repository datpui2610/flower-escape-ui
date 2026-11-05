# Flower Escape UI

Separate scenes:

- `Assets/Scenes/FlowerEscape_Main.unity` — Main, 144 editable UI Images.
- `Assets/Scenes/FlowerEscape_BuyGear.unity` — Buy Gear, 105 editable UI Images.

Combined reference scene: `Assets/Scenes/FlowerEscape_TwoScreens.unity`.

Both screens are 450 x 1050 (9:21). Main contains 144 separate UI Images; Buy Gear contains 105. No gameplay logic is attached.

To move an element, open the **Scene** tab, select its named child in the **Hierarchy**, and press **T** for the Rect tool. You can also edit **RectTransform > Pos X / Pos Y** in the Inspector. The Game tab only previews the result.

Each PNG is a separate exported Figma layer. Text is rasterized: its position and size can be edited, but changing the words requires replacing the sprite.

Source: https://www.figma.com/design/bNxsEAwP5cQAH8B696OGb5/
