using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RuleTileFixer : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        // 0: enable this object
        // 1: duplicate the ruletile template folder
        // 2: rename the aseprite from ruletile -> whatever is appropriate (usually xxxmap_tiles), and re-slice the sprites (this is to rename the individual sprites)
        // 3: rename the rule tile to whatever is appropriate as well (usually xxxmap_ruletile)
        // 4: rename the paths in this file
        // 5: run this script (just start and stop the game, may have to run it a few times - check the ruletile if the tiles are updated or not)
        // 6: disable this object (V IMPORTANT YOU MIGHT OVERWRITE YOUR ART LATER IF THIS RUNS AGAIN)

        // find the new sprites 
        List<Sprite> newSprites = new();
        // TODO: CHANGE THE PATH TO THE CORRECT ASSET HERE (aseprite file)
        // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
        Object[] aseprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Tiles/boostmap2_ruletile/boostmap2_tiles.aseprite");
        foreach (Object s in aseprite)
        {
            // TODO: CHANGE THE PATH TO THE CORRECT ASSET HERE, ENDING WITH AN UNDERSCORE (aseprite file without extension + underscore)
            // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
            if (s.name.StartsWith("boostmap2_tiles_"))
            {
                newSprites.Add((Sprite) s);
            }
        }

        // get the ruletile
        // TODO: CHANGE THE PATH TO THE CORRECT ASSET HERE (the renamed ruletile)
        // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
        RuleTile ruleTile = (RuleTile)AssetDatabase.LoadAssetAtPath("Assets/Sprites/Tiles/boostmap2_ruletile/boostmap2_ruletile.asset", typeof(RuleTile));

        // for each rule, change the sprite file
        List<RuleTile.TilingRule> rules = ruleTile.m_TilingRules;
        foreach (RuleTile.TilingRule rule in rules)
        {
            string name = rule.m_Sprites[0].name;
            rule.m_Sprites[0] = newSprites[int.Parse(name[18..])]; // 18th index because "ruletile_template_" is 18 chars long
        }
    }
}
