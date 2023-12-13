using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class RuleTileFixer : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        // 1: duplicate the ruletile template folder
        // 2: rename the aseprite from ruletile -> whatever is appropriate, and re-slice the sprites
        // 3: rename the paths in this file
        // 4: run this script

        // find the new sprites 
        List<Sprite> newSprites = new();
        // TODO: CHANGE THE PATH TO THE CORRECT ASSET HERE
        // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
        Object[] aseprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Tiles/stunmap_ruletile/stunmap_tiles.aseprite");
        foreach (Object s in aseprite)
        {
            // TODO: CHANGE THE PATH TO THE CORRECT ASSET HERE, ENDING WITH AN UNDERSCORE
            // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
            if (s.name.StartsWith("stunmap_tiles_"))
            {
                newSprites.Add((Sprite) s);
            }
        }

        // get the ruletile
        // TODO: CHANGE THE PATH TO THE CORRECT ASSET HERE
        // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
        RuleTile ruleTile = (RuleTile)AssetDatabase.LoadAssetAtPath("Assets/Sprites/Tiles/stunmap_ruletile/stunmap_ruletile.asset", typeof(RuleTile));

        // for each rule, change the sprite file
        List<RuleTile.TilingRule> rules = ruleTile.m_TilingRules;
        foreach (RuleTile.TilingRule rule in rules)
        {
            string name = rule.m_Sprites[0].name;
            rule.m_Sprites[0] = newSprites[int.Parse(name[18..])]; // 18th index because "ruletile_template_" is 18 chars long
        }
    }
}
