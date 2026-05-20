oklch() is a new way to define CSS colors. In oklch(L C H) or oklch(L C H / a), each item corresponds as follows:

L is perceived lightness (0-1). “Perceived” means that it has consistent lightness for our eyes, unlike L in hsl().
C is chroma, from gray to the most saturated color.
H is the hue angle (0-360).
a is opacity (0-1 or 0-100%).
a:hover {
color:   oklch(0.45 0.26 264); /* blue */
color:   oklch(1 0 0);         /* white */
color:   oklch(0 0 0 / 50%);   /* black with 50% opacity */
}
The benefits of OKLCH:

OKLCH frees designers from the need to manually choose every color. Designers can define a formula, choose a few colors, and an entire design system palette is automatically generated.
OKLCH can be used for wide-gamut P3 colors. For instance, new devices (like those from Apple) can display more colors than old sRGB monitors, and we can use OKLCH to specify these new colors.
Unlike hsl(), OKLCH is better for color modifications and palette generation. It uses perceptual lightness, so no more unexpected results, like we had with darken() in Sass.
