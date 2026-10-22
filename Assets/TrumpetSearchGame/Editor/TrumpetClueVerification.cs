using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class TrumpetClueVerification
    {
        [InitializeOnLoadMethod] static void Requested()
        {
            EditorApplication.delayCall += () => {
                if (!File.Exists(".utmp/trumpet-clue-test")) return;
                File.Delete(".utmp/trumpet-clue-test"); Run();
            };
        }
        [MenuItem("Tools/Jericho Prototype/Verify Trumpet Clues")]
        static void Run()
        {
            var root = new GameObject("Temporary clue verification");
            try
            {
                var game = root.AddComponent<TrumpetSearchGame>();
                game.trumpets = new TrumpetCollectible[7];
                for (int i = 0; i < 7; i++)
                {
                    var horn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    horn.transform.SetParent(root.transform);
                    game.trumpets[i] = horn.AddComponent<TrumpetCollectible>();
                    game.trumpets[i].owner = game;
                    game.trumpets[i].ResetCollectible();
                }
                Verify(game);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/trumpet-clue-test.txt", "PASS: instructions gate; all seven wrong answers stay hidden and uncollectible; correct answers reveal only their own horn; duplicate prevention; seven-horn completion; restart locks and hides every horn.");
            }
            catch (Exception e) { File.WriteAllText("Logs/trumpet-clue-test.txt", e.ToString()); Debug.LogException(e); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        public static void Verify(TrumpetSearchGame game)
        {
            int[] correct = { 1,0,2,0,1,2,0 };
            Require(!game.AnswerClue(0,1) && !game.Collect(game.trumpets[0]), "Instructions bypass");
            game.BeginSearch();
            for (int i = 0; i < 7; i++)
            {
                var horn = game.trumpets[i];
                Require(!game.Collect(horn), "Locked horn collected");
                Require(!game.AnswerClue(i,(correct[i]+1)%3) && !horn.Revealed && !game.Collect(horn), "Wrong answer unlocked horn");
                Require(game.AnswerClue(i,correct[i]) && horn.Revealed, "Correct answer failed");
                for (int j = i+1; j < 7; j++) Require(!game.trumpets[j].Revealed, "Another horn revealed");
                Require(game.Collect(horn) && !game.Collect(horn), "Collection or duplicate gate failed");
            }
            Require(game.Complete && game.Found == 7, "Completion failed");
            game.RestartSearch(); game.BeginSearch();
            foreach (var horn in game.trumpets)
            {
                Require(!horn.Revealed && !horn.Collected && !game.Collect(horn), "Reset gate failed");
                foreach (var renderer in horn.GetComponentsInChildren<Renderer>(true)) Require(!renderer.enabled || !renderer.gameObject.activeInHierarchy, "Reset left visible horn");
            }
            Require(game.Found == 0 && !game.Complete, "Reset count failed");
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}

