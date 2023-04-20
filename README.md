# Style Mask Helper

Style Mask Helper adds a variety of "mask" entities that change certain aspects of the level's visuals for just a specified area, and optionally smoothly fade them!

This mod currently contains:

- Styleground Masks
- Lighting Masks
- Color Grade Masks
- Bloom Masks

## Usage

### Styleground Masks

To mask a styleground, give it a tag with the `mask_` prefix, and use the same tag (without the prefix) inside the styleground mask options. For example, if you give a styleground the `mask_stars` tag, you'll want to put `stars` as the "Tag" option for your styleground mask.

Stylegrounds with a `mask_` tag will not be visible outside of a styleground mask!


### Custom Fade

**Note: This does not currently work in many cases! Use at your own risk in case fixes change stuff.**

All masks can be given an image for their shape instead of just a rectangle using the "Custom" fade mode by placing an image inside the `Gameplay/fademasks` graphics folder, and setting the "Custom Fade" option to the image's path (relative to the `fademasks` folder).

The image should only consist of transparency and white, where the white is where your mask will render. The image will be stretched to fit your mask, so make sure you have the sizes right!

The mod comes with a few [default fade masks](Graphics/Atlases/Gameplay/fademasks/) you can use, so check those for examples.

## Screenshots

**Styleground Mask**
![Screenshot](.github/images/stylegroundMask.png)

**Lighting Mask**
![Screenshot](.github/images/lightingMask.png)

**Color Grade Mask**
![Screenshot](.github/images/colorGradeMask.png)

**Bloom Mask**
![Screenshot](.github/images/bloomMask.png)

## Known Issues

- Custom Fade option has a variety of issues:
  - Styleground masks dont support transparent stylegrounds (big issue for foregrounds)
  - Color grade masks look ugly with partial transparency
  - Bloom masks just dont work with it
  - Lighting masks dont work with it either (and also break light sources in their area)

## Building

Building the project requires a modified Celeste executable, and since I dont know if I can commit that to a public GitHub you need to make your own (also these are just Windows instructions)

### 1. Download an assembly publicizer

Download **one** of the following:

- [NStrip](https://github.com/bbepis/NStrip)
- [BepInEx Assembly Publicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer)

NStrip is a small portable executable that you can download and place next to the file you want to publicize. \
BepInEx is convenient if you have the dotnet CLI.

### 2. Create a publicized executable

With the publicizer installed and/or placed next to your Celeste executable, run the following command:

**BepInEx**  
`assembly-publicizer --strip Celeste.exe`

**NStrip**  
`NStrip.exe -p Celeste.exe` 

### 3. Place inside the mod

Move your newly generated `Celeste-publicized.exe` or `Celeste-nstrip.exe` to the `lib-stripped` folder inside the Style Mask Helper code, and rename it to `Celeste.exe`.

The project should now be able to access private Celeste variables without errors!