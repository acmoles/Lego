using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegoColors
{

    public enum Id
    {
        // Universal.
        White = 1,
        BrickYellow = 5,
        BrightRed = 21,
        BrightBlue = 23,
        BrightYellow = 24,
        Black = 26,
        DarkGreen = 28,
        ReddishBrown = 192,
        MediumStoneGrey = 194,
        DarkStoneGrey = 199,

        // Generic.
        Nougat = 18,
        BrightGreen = 37,
        MediumBlue = 102,
        BrightOrange = 106,
        BrightYellowGreen = 119,
        EarthBlue = 140,
        EarthGreen = 141,
        NewDarkRed = 154,
        BrightPurple = 221,
        LightPurple = 222,
        MediumAzur = 322,
        MediumLavender = 324,
    }

    private readonly static Dictionary<Id, Color> idToColourGuide = new Dictionary<Id, Color>()
    {
        // Universal.
        { Id.White,                             new Color32(244, 244, 244, 255) },
        { Id.BrickYellow,                       new Color32(204, 185, 141, 255) },
        { Id.BrightRed,                         new Color32(180, 0, 0, 255)     },
        { Id.BrightBlue,                        new Color32(30, 90, 168, 255)   },
        { Id.BrightYellow,                      new Color32(250, 200, 10, 255)  },
        { Id.Black,                             new Color32(0, 0, 0, 255)       },
        { Id.DarkGreen,                         new Color32(0, 133, 43, 255)    },
        { Id.ReddishBrown,                      new Color32(95, 49, 9, 255)     },
        { Id.MediumStoneGrey,                   new Color32(150, 150, 150, 255) },
        { Id.DarkStoneGrey,                     new Color32(100, 100, 100, 255) },

        // Generic.
        { Id.Nougat,                            new Color32(187, 128, 90, 255)  },
        { Id.BrightGreen,                       new Color32(88, 171, 65, 255)   },
        { Id.MediumBlue,                        new Color32(115, 150, 200, 255) },
        { Id.BrightOrange,                      new Color32(214, 121, 35, 255)  },
        { Id.BrightYellowGreen,                 new Color32(165, 202, 24, 255)  },
        { Id.EarthBlue,                         new Color32(25, 25, 50, 255)    },
        { Id.EarthGreen,                        new Color32(0, 69, 26, 255)     },
        { Id.NewDarkRed,                        new Color32(114, 0, 18, 255)    },
        { Id.BrightPurple,                      new Color32(200, 80, 155, 255)  },
        { Id.LightPurple,                       new Color32(255, 158, 205, 255) },
        { Id.MediumAzur,                        new Color32(104, 195, 226, 255) },
        { Id.MediumLavender,                    new Color32(154, 118, 174, 255) },
    };

    public static Color GetColour(Id id)
    {
        if (idToColourGuide.ContainsKey(id))
        {
            //Debug.Log("Hex of color " + id + " - " + ColorUtility.ToHtmlStringRGBA(idToColourGuide[id]));
            return idToColourGuide[id];
        }
        else
        {
            Debug.LogError("Moulding color id " + id + " is missing a colour");
            return Color.black;
        }
    }

    public static Color GetColour(string id)
    {
        try
        {
            return GetColour((Id)Enum.Parse(typeof(Id), id));
        }
        catch
        {
            Debug.LogErrorFormat("Invalid moulding colour id {0}", id);
            return Color.black;
        }
    }

    public static Color GetColour(int id)
    {
        return GetColour(id.ToString());
    }

}
