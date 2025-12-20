using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace RabbitVsMole
{
    public class GameSceneManager : MonoBehaviour
    {
        public enum SceneType
        {
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
            GamePlay_Duel,

            [Description("m_test_bot")]
            Debug_BotTest
        };

        public static SceneType CurrentScene
        {
            get;
            private set;
        }

        /// <summary>
        /// Changes the current scene to the specified <see cref="SceneType"/> and invokes optional callbacks during the
        /// scene transition.
        /// </summary>
        /// <remarks>The method uses the <see cref="DescriptionAttribute"/> of the specified <paramref
        /// name="scene"/> to determine the scene name to load. If the <paramref name="scene"/> does not have a <see
        /// cref="DescriptionAttribute"/>, the method does not perform any action.</remarks>
        /// <param name="scene">The scene to load. Must be a valid <see cref="SceneType"/> value decorated with a <see
        /// cref="DescriptionAttribute"/> specifying the scene name.</param>
        /// <param name="OnSceneLoad">An optional callback invoked after the scene has been loaded but before it is activated. Receives the loaded
        /// <see cref="Scene"/> as a parameter.</param>
        /// <param name="OnSceneStart">An optional callback invoked when the scene starts after loading is complete.</param>
        /// <param name="OnSceneShow">An optional callback invoked when the scene becomes visible to the user.</param>
        public static void ChangeScene(
            SceneType scene,
            UnityAction<Scene> OnSceneLoad = null,
            UnityAction OnSceneStart = null,
            UnityAction OnSceneShow = null)
        {
            var description = scene.GetType()
                .GetField(scene.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (description.Length > 0)
            {
                CurrentScene = scene;
                SceneLoader.ChangeScene(
                    ((DescriptionAttribute)description[0]).Description,
                    OnSceneLoad,
                    OnSceneStart,
                    OnSceneShow,
                    (i) => { DebugHelper.Log(null, $"loading: {i}%"); });
            }
        }
    }
}