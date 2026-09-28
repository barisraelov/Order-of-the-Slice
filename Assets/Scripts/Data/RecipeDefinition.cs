using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "Order of the Slice/Recipe Definition")]
public class RecipeDefinition : ScriptableObject
{
    public string displayName = "Recipe";
    public IngredientId[] orderedIngredients = Array.Empty<IngredientId>();
}
