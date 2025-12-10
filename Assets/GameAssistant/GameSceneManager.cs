using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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

    public static SceneType CurrentScene
    {
        get;
        private set;
    }

    /// <summary>
    /// Changes the current scene to the specified <see cref="SceneType"/>.
    /// </summary>
    /// <remarks>This method sets the active scene and initiates the scene loading process. Optional callbacks
    /// can be provided to execute custom logic when the scene starts or after the scene is loaded. The scene is
    /// identified using the <see cref="DescriptionAttribute"/> associated with the <paramref name="scene"/>
    /// value.</remarks>
    /// <param name="scene">The scene to switch to. Must be a valid <see cref="SceneType"/> value decorated with a <see
    /// cref="DescriptionAttribute"/>.</param>
    /// <param name="OnSceneStart">An optional callback invoked when the new scene starts. This is Invoked AFTER animation finish. If <see langword="null"/>, no action is taken.</param>
    /// <param name="OnSceneLoad">An optional callback invoked after the scene is loaded, receiving the loaded <see cref="Scene"/> as a parameter.
    /// If <see langword="null"/>, no action is taken.</param>
    public static void ChangeScene(SceneType scene, UnityAction OnSceneStart = null, UnityAction<Scene> OnSceneLoad = null)
    {
        var description = scene.GetType()
            .GetField(scene.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (description.Length > 0)
        {
            CurrentScene = scene;
            SceneLoader.ChangeScene(((DescriptionAttribute)description[0]).Description, OnSceneLoad, OnSceneStart, (i) => { Debug.Log($"loading: {i}%"); });
        }
    }
}