using Godot;

namespace Knight.Presentation;

public partial class CanyonBackdropVisual
{
    private void DrawEntranceNaturalApproach(float width, float height)
    {
        Vector2 scale = new(width / 1280f, height / 720f);
        // Recessed background only. The new Solid skins own the enclosing
        // rock silhouette; do not retain old diagonal stair sockets as false routes.
        PaintEntrancePolygon(scale,[new(48,80),new(600,80),new(600,400),
            new(1280,400),new(1280,636),new(48,636)],
            [new("26383b",.7f)], [new("5a6253",.55f),new("596052",.46f),
                new("334447",.5f),new("172c34",.65f),new("132831",.8f),new("283b3b",.5f)]);
        PaintEntrancePolygon(scale,[new(85,100),new(540,80),new(470,375),
            new(342,560),new(98,603)], [new("7e8167",.12f)]);
        PaintEntrancePolygon(scale,[new(110,332),new(226,260),new(356,286),
            new(444,377),new(417,530),new(115,601)], [new("405253",.25f)]);
        PaintEntrancePolygon(scale,[new(616,456),new(792,418),new(970,452),
            new(1190,431),new(1280,470),new(1280,636),new(600,636)],
            [new("0f242d",.25f)]);
    }

    private void DrawEntranceCavern(float width, float height)
    {
        Vector2 scale = new(width / 1280f, height / 720f);
        Color cool = new("39494e", .62f);
        Color coolShade = new("1b2b32", .42f);
        Color clear = new("39494e", 0f);

        // An offset, irregular recession, not a second architectural doorway. Its lower-left
        // shoulder dissolves before the first combat position (500..620, 470..560).
        PaintEntrancePolygon(scale, [new(310f, 285f), new(457f, 165f),
            new(766f, 142f), new(947f, 232f), new(884f, 365f),
            new(717f, 442f), new(627f, 458f), new(409f, 391f)],
            [coolShade, cool, cool, coolShade, coolShade, clear, clear, coolShade],
            [new("263c49", .62f), new("405966", .78f), new("48616a", .82f),
                new("2c434f", .59f), new("1c323f", .42f),
                new("1c323f", 0f), new("1c323f", 0f), new("233a46", .28f)]);

        // Broad skylight enters from the upper right. Vertex alpha fades out above the combat
        // lane; no additive pass, particles, bright cyan cue, or whole-scene cover is involved.
        Color light = new("8a8a75", .23f);
        Color middleLight = new("737d78", .12f);
        Color noLight = new("737d78", 0f);
        PaintEntrancePolygon(scale, [new(986f, 20f), new(1170f, 38f),
            new(932f, 334f), new(775f, 426f), new(654f, 400f), new(825f, 198f)],
            [light, light, noLight, noLight, noLight, middleLight],
            [new("a39a78", .27f), new("a39a78", .25f), new("75837e", 0f),
                new("75837e", 0f), new("75837e", 0f), new("8a9181", .14f)]);

        // Rock closes the sides, not the playable route. Broad facets give these enclosing
        // masses depth without outlining every slab or adding repetitive surface noise.
        PaintEntrancePolygon(scale, [new(0f, 0f), new(640f, 0f), new(550f, 45f),
            new(347f, 64f), new(205f, 125f), new(101f, 233f), new(0f, 291f)],
            [new("111e24", .95f)],
            [new("101c23", .98f), new("17282d", .97f), new("2b3e40", .88f),
                new("26393c", .92f), new("203238", .97f),
                new("15262f", .98f), new("101d26", .98f)]);
        PaintEntrancePolygon(scale, [new(0f, 144f), new(80f, 199f), new(124f, 348f),
            new(97f, 452f), new(155f, 543f), new(116f, 636f), new(0f, 688f)],
            [new("121f25", .94f)],
            [new("12202a", .98f), new("23363b", .96f), new("2b4042", .93f),
                new("1c3039", .96f), new("293c3e", .95f),
                new("172b34", .98f), new("101e27", .98f)]);
        PaintEntrancePolygon(scale, [new(0f, 231f), new(51f, 248f), new(84f, 351f),
            new(63f, 453f), new(104f, 524f), new(73f, 594f), new(0f, 628f)],
            [new("28383a", .42f)],
            [new("253942", .34f), new("506058", .43f), new("485c56", .40f),
                new("2a4148", .29f), new("3d5350", .38f),
                new("233b44", .24f), new("172c36", .18f)]);
        // Entrance alone owns this broken shoulder across world x=1280. Descent is a
        // required neighbor; no second translucent copy or sector-height cutoff is needed.
        PaintEntrancePolygon(scale, [new(1137f, 0f), new(1450f, 0f),
            new(1390f, 94f), new(1510f, 250f), new(1470f, 440f),
            new(1560f, 590f), new(1500f, 720f), new(1390f, 790f),
            new(1350f, 690f), new(1290f, 614f), new(1241f, 502f),
            new(1194f, 389f), new(1236f, 244f), new(1180f, 128f)],
            [new("152329", .84f)],
            [new("424d43", .89f), new("2e403f", .91f), new("354842", .89f),
                new("21373c", .87f), new("1b3039", .86f), new("253b3f", .84f),
                new("1c3139", .80f), new("182c34", .64f), new("20343b", .70f),
                new("2b3e40", .83f), new("344742", .88f), new("415046", .91f),
                new("52604f", .92f), new("65705a", .94f)]);
        PaintEntrancePolygon(scale, [new(1162f, 31f), new(1232f, 93f),
            new(1310f, 166f), new(1354f, 286f), new(1301f, 251f),
            new(1242f, 148f), new(1211f, 112f)], [new("3d4642", .36f)],
            [new("8c8866", .48f), new("99906b", .43f), new("7b7b5e", .29f),
                new("536052", .09f), new("536052", .08f),
                new("76795e", .22f), new("92906d", .38f)]);

        // Separate banks preserve the real 320..400 opening. Neither silhouette has a bright
        // horizontal rim, and all lower-edge massing stays behind the authored platform skins.
        PaintEntrancePolygon(scale, [new(0f, 665f), new(114f, 650f), new(195f, 666f),
            new(271f, 657f), new(309f, 704f), new(312f, 720f), new(0f, 720f)],
            [new("122127", .94f)]);
        PaintEntrancePolygon(scale, [new(406f, 706f), new(464f, 675f), new(494f, 693f),
            new(581f, 674f), new(682f, 702f), new(803f, 677f),
            new(951f, 709f), new(1106f, 674f), new(1261f, 703f),
            new(1338f, 682f), new(1401f, 736f), new(1366f, 760f),
            new(404f, 720f)], [new("16262b", .86f)]);
    }

    private void DrawEntranceFoundations(float width, float height)
    {
        Vector2 scale = new(width / 1280f, height / 720f);
        Color masonry = new("31413f", .62f);
        Color wornFace = new("43504a", .37f);
        Color darkJoint = new("1b2b30", .55f);

        // A canted ruin rises behind the stair route. Its fractured diagonal crown has no
        // readable horizontal top. This is one connected mass, not seven floating posts.
        PaintEntrancePolygon(scale, [new(669f, 720f), new(700f, 634f), new(766f, 588f),
            new(816f, 500f), new(906f, 430f), new(957f, 348f),
            new(1034f, 291f), new(1084f, 213f), new(1178f, 161f),
            new(1265f, 143f), new(1321f, 198f), new(1293f, 262f),
            new(1368f, 331f), new(1309f, 382f), new(1263f, 357f),
            new(1176f, 427f), new(1130f, 510f), new(1039f, 579f),
            new(998f, 652f), new(913f, 720f)], [masonry],
            [new("263c43", .66f), new("364b4c", .74f), new("41544f", .79f),
                new("485a50", .81f), new("526151", .82f), new("5a6654", .83f),
                new("646c57", .84f), new("6f735b", .84f), new("73765e", .85f),
                new("626b56", .82f), new("43564d", .77f), new("34494a", .75f),
                new("2b4147", .71f), new("263c44", .69f), new("2e4548", .72f),
                new("30474b", .73f), new("283e47", .70f), new("233a44", .67f),
                new("1e333e", .61f), new("1b303a", .57f)]);
        PaintEntrancePolygon(scale, [new(700f, 634f), new(766f, 588f), new(816f, 500f),
            new(906f, 430f), new(957f, 348f), new(1034f, 291f),
            new(1084f, 213f), new(1178f, 161f), new(1210f, 164f),
            new(1102f, 270f), new(976f, 410f), new(836f, 584f), new(729f, 686f)],
            [wornFace],
            [new("53665b", .27f), new("5e6c5b", .30f), new("6a755e", .34f),
                new("758067", .36f), new("7c8469", .39f), new("85886b", .41f),
                new("8d8e6e", .43f), new("999574", .45f), new("a09975", .40f),
                new("737e66", .24f), new("5a6b5e", .20f),
                new("3e5853", .16f), new("334b4b", .12f)]);
        PaintEntrancePolygon(scale, [new(881f, 720f), new(918f, 563f), new(1046f, 401f),
            new(1159f, 304f), new(1183f, 340f), new(1067f, 472f), new(982f, 720f)],
            [darkJoint],
            [new("172d38", .64f), new("1b333e", .61f), new("233c45", .57f),
                new("304a4c", .43f), new("273e46", .53f),
                new("19313d", .66f), new("152b37", .73f)]);

        if (UseCanyonDepthStaging &&
            SectorId == Knight.Prologue.Seamless.PrologueSectorId.CanyonEntrance)
        {
            // Socket heads overlap the real undersides by one pixel behind the skins.
            // The existing diagonal feet still join the same ruin, never the entry gap.
            DrawEntranceSupportContact(scale, new(531f, 595f), new(580f, 595f),
                new(737f, 693f), new(695f, 716f));
            DrawEntranceSupportContact(scale, new(638f, 515f), new(680f, 515f),
                new(839f, 610f), new(790f, 647f));
            DrawEntranceSupportContact(scale, new(751f, 435f), new(789f, 435f),
                new(896f, 503f), new(868f, 537f));
            DrawEntranceSupportContact(scale, new(861f, 355f), new(895f, 355f),
                new(996f, 421f), new(968f, 453f));
            DrawEntranceSupportContact(scale, new(971f, 275f), new(1014f, 275f),
                new(1096f, 318f), new(1071f, 351f));
            DrawEntranceSupportContact(scale, new(1079f, 195f), new(1121f, 195f),
                new(1167f, 238f), new(1143f, 264f));
            DrawEntranceSupportContact(scale, new(1179f, 139f), new(1202f, 139f),
                new(1248f, 195f), new(1224f, 220f));
        }
        else
        {
        // Quiet diagonal sockets begin BELOW the real 40px platform bodies. Two broad spans
        // and three upper braces connect treads to the ruin; none bridges the entry gap.
        PaintEntrancePolygon(scale, [new(531f, 601f), new(580f, 601f), new(737f, 693f),
            new(695f, 716f)], [new("354540", .38f)]);
        PaintEntrancePolygon(scale, [new(638f, 521f), new(680f, 521f), new(839f, 610f),
            new(790f, 647f)], [new("354540", .33f)]);
        PaintEntrancePolygon(scale, [new(751f, 441f), new(789f, 441f), new(896f, 503f),
            new(868f, 537f)], [new("354540", .31f)]);
        PaintEntrancePolygon(scale, [new(861f, 361f), new(895f, 361f), new(996f, 421f),
            new(968f, 453f)], [new("354540", .29f)]);
        PaintEntrancePolygon(scale, [new(971f, 281f), new(1014f, 281f), new(1096f, 318f),
            new(1071f, 351f)], [new("354540", .29f)]);
        PaintEntrancePolygon(scale, [new(1079f, 201f), new(1121f, 201f), new(1167f, 238f),
            new(1143f, 264f)], [new("354540", .26f)]);
        PaintEntrancePolygon(scale, [new(1179f, 145f), new(1202f, 145f), new(1248f, 195f),
            new(1224f, 220f)], [new("354540", .26f)]);
        }

        // Isolated low bridge remnant and far-bank footings, all inside their own bank.
        PaintEntrancePolygon(scale, [new(264f, 513f), new(293f, 513f), new(244f, 635f),
            new(225f, 659f), new(226f, 608f)], [new("293b39", .42f)]);
        PaintEntrancePolygon(scale, [new(412f, 681f), new(477f, 681f), new(507f, 713f),
            new(420f, 720f)], [new("35413b", .54f)]);
    }

    private void DrawEntranceSupportContact(Vector2 scale, Vector2 headLeft, Vector2 headRight,
        Vector2 footRight, Vector2 footLeft)
    {
        // Broad face values recede into the original ruin; no horizontal landing is added.
        PaintEntrancePolygon(scale, [headLeft, headRight, footRight, footLeft],
            [new("596052", .65f), new("485348", .63f),
                new("354943", .43f), new("3e5047", .48f)]);
        Vector2 sideHead = headRight.Lerp(headLeft, .23f);
        Vector2 sideFoot = footRight.Lerp(footLeft, .23f);
        PaintEntrancePolygon(scale, [sideHead, headRight, footRight, sideFoot],
            [new("263a3d", .49f), new("21353a", .56f),
                new("243a3d", .33f), new("2a403f", .29f)]);
        // A short dark compression shade, not a bright rim or a traversable shelf.
        PaintEntrancePolygon(scale, [headLeft, headRight,
                headRight.Lerp(footRight, .08f), headLeft.Lerp(footLeft, .08f)],
            [new("1e3034", .60f), new("1e3034", .60f),
                new("2c403f", .25f), new("2c403f", .25f)]);
    }

    private void DrawEntranceBelfry(float width, float height)
    {
        Vector2 scale = new(width / 1280f, height / 720f);
        Color pier = new("35413f", .86f);
        Color pierFace = new("48514a", .59f);

        // A surviving left pier, snapped right foot and leaning crown identify a ruined belfry.
        // No enclosing portal, basket, gear, or repeated second landmark is painted here.
        PaintEntrancePolygon(scale, [new(103f, 630f), new(122f, 312f), new(113f, 291f),
            new(147f, 257f), new(177f, 281f), new(161f, 371f), new(157f, 610f),
            new(185f, 636f)], [pier],
            [new("273e43", .92f), new("43584f", .94f), new("4c5d51", .95f),
                new("6d715a", .96f), new("77795f", .95f), new("526151", .91f),
                new("31494a", .88f), new("293f44", .88f)]);
        PaintEntrancePolygon(scale, [new(125f, 329f), new(146f, 299f), new(148f, 475f),
            new(131f, 584f), new(119f, 609f)], [pierFace],
            [new("626f59", .50f), new("a0946c", .59f), new("737d62", .39f),
                new("435c55", .21f), new("2e484d", .12f)]);
        PaintEntrancePolygon(scale, [new(134f, 286f), new(169f, 253f), new(335f, 239f),
            new(355f, 261f), new(308f, 276f), new(311f, 294f), new(188f, 312f)], [pier],
            [new("3a514b", .93f), new("606d55", .96f), new("8a8662", .96f),
                new("6d7156", .94f), new("435a50", .91f),
                new("2b4446", .91f), new("2a4345", .93f)]);
        PaintEntrancePolygon(scale, [new(169f, 253f), new(335f, 239f), new(355f, 261f),
            new(211f, 284f), new(172f, 280f)], [pierFace],
            [new("818162", .33f), new("a4976c", .56f), new("978c64", .43f),
                new("5d715b", .22f), new("4c6355", .17f)]);
        PaintEntrancePolygon(scale, [new(382f, 582f), new(416f, 552f), new(444f, 579f),
            new(451f, 640f), new(403f, 638f)], [pier],
            [new("3b514b", .89f), new("657059", .91f), new("526451", .91f),
                new("273f44", .87f), new("233b42", .87f)]);
        DrawLine(new Vector2(139f, 454f) * scale, new Vector2(158f, 437f) * scale,
            new Color("17272c", .73f), 4f, true);

        // Single chipped bronze bell with a curved shoulder, dark mouth and fractured lip.
        // It lives left of the combat lane, so its quiet warm patina cannot mimic an attack cue.
        DrawLine(new Vector2(252f, 285f) * scale, new Vector2(244f, 344f) * scale,
            new Color("575244", .64f), 5f, true);
        PaintEntrancePolygon(scale, [new(222f, 337f), new(260f, 334f), new(282f, 350f),
            new(288f, 381f), new(305f, 414f), new(331f, 439f),
            new(301f, 453f), new(282f, 434f), new(267f, 451f),
            new(206f, 457f), new(169f, 448f), new(188f, 421f),
            new(205f, 382f), new(207f, 354f)], [new("625b48", .87f)],
            [new("7b775c", .95f), new("a28b5f", .97f), new("af9466", .97f),
                new("9a855d", .96f), new("817653", .95f), new("706c50", .94f),
                new("485749", .94f), new("4b5a49", .95f), new("415347", .95f),
                new("344a44", .96f), new("293f41", .95f), new("3b5149", .94f),
                new("546451", .95f), new("69705a", .95f)]);
        PaintEntrancePolygon(scale, [new(260f, 334f), new(282f, 350f), new(288f, 381f),
            new(305f, 414f), new(331f, 439f), new(301f, 453f), new(282f, 434f),
            new(261f, 405f), new(251f, 365f)], [new("3d443b", .78f)],
            [new("b29a6b", .26f), new("b99e6c", .35f), new("ab9265", .29f),
                new("8b7e5b", .17f), new("647052", .10f), new("304940", .25f),
                new("3a5043", .20f), new("607054", .16f), new("a49268", .27f)]);
        PaintEntrancePolygon(scale, [new(183f, 442f), new(211f, 432f), new(257f, 431f),
            new(270f, 440f), new(261f, 449f), new(206f, 453f)],
            [new("1d2b2e", .87f)]);
        DrawPolyline([new Vector2(210f, 369f) * scale, new Vector2(225f, 346f) * scale,
            new Vector2(251f, 343f) * scale], new Color("89816a", .47f), 4f, true);
        DrawLine(new Vector2(187f, 433f) * scale, new Vector2(256f, 426f) * scale,
            new Color("7b725b", .53f), 4f, true);
        DrawPolyline([new Vector2(270f, 391f) * scale, new Vector2(255f, 413f) * scale,
            new Vector2(265f, 430f) * scale], new Color("253330", .78f), 5f, true);
        PaintEntrancePolygon(scale, [new(228f, 438f), new(242f, 440f), new(243f, 476f),
            new(233f, 482f), new(226f, 474f)], [new("454739", .69f)]);
        PaintEntrancePolygon(scale, [new(170f, 600f), new(211f, 592f), new(224f, 614f),
            new(249f, 621f), new(247f, 634f), new(190f, 632f)], [new("35423c", .58f)]);
    }

    private void PaintEntrancePolygon(Vector2 scale, Vector2[] profile, Color[] colors,
        Color[] stagedColors = null)
    {
        for (int index = 0; index < profile.Length; index++)
            profile[index] *= scale;
        // The same retained contours receive broad value interpolation. Sample/default
        // consumers keep the exact original color arrays and draw call.
        DrawPolygon(profile, UseCanyonDepthStaging &&
            SectorId == Knight.Prologue.Seamless.PrologueSectorId.CanyonEntrance && stagedColors != null
                ? stagedColors : colors);
    }
}
