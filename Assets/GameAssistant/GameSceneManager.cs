using System.ComponentModel;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    public enum SceneType {
        [Description("m_main_menu")]
        MainMenu,

        [Description("m_rabbit_solo")]
        Gameplay_RabbitSolo,

        [Description("m_mole_solo")]
        GamePlay_MoleSolo,

        [Description("m_rabbit_challeange")]
        GamePlay_RabbitChalleange,

        [Description("m_mole_challeange")]
        GamePlay_MoleChalleange,

        [Description("m_duel")]
        GamePlay_Duel
    };

    public static void ChangeScene(SceneType scene)
    {
        var description = scene.GetType()
            .GetField(scene.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (description.Length > 0)
        {
            SceneLoader.ChangeScene(((DescriptionAttribute)description[0]).Description);
        }
    }
}