using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleAutomator : MonoBehaviour
{
    private PuzzlePiece[] powerablePuzzlePieces;

    private ComputerBehavior[] computers;
    private GeneratorBehavior generator;
    private PressurePlate[] pressurePlates;
    private PuzzleButtonBehavior[] puzzleButtons;

    private ComputerNetwork computerNetwork;

    private int totalPowerSources = 0;


    // Put this on a parent object, and we'll try to take care of the rest.
    // Simple cases only - no toggle lights, no multiple generators, no computers with generators, etc. If you have a more complex case, you'll have to do it yourself for now.

    // Start is called before the first frame update
    void Start()
    {
        powerablePuzzlePieces = GetComponentsInChildren<PuzzlePiece>();
        computers = GetComponentsInChildren<ComputerBehavior>();
        generator = GetComponentInChildren<GeneratorBehavior>();
        pressurePlates = GetComponentsInChildren<PressurePlate>();
        computerNetwork = GetComponent<ComputerNetwork>();
        puzzleButtons = GetComponentsInChildren<PuzzleButtonBehavior>();

        if (generator == null && pressurePlates.Length == 0 && powerablePuzzlePieces.Length > 1)
        {
            Debug.LogError("PuzzleAutomator: No generator or pressure plates found, but multiple powerable puzzle pieces found. Unhandled case.");
        }

        if (generator != null)
        {
            prepGenerator();
        }

        if (computers.Length > 0)
        {
            if (generator != null)
            {
                Debug.LogError("PuzzleAutomator: Computers found, but generator found as well. Unhandled case.");
                return;
            }

            prepComputers();
        }

        if (pressurePlates.Length > 0)
        {
            if (generator != null)
            {
                Debug.LogError("PuzzleAutomator: Pressure plates found, but generator found as well. Unhandled case.");
                return;
            }

            prepPressurePlates();
        }

        if (puzzleButtons.Length > 0)
        {
            if (generator != null)
            {
                Debug.LogError("PuzzleAutomator: Puzzle buttons found, but generator found as well. Unhandled case.");
                return;
            }

            prepButtons();
        }

        handlePuzzlePieceLights();
    }

    private void handlePuzzlePieceLights()
    {
        foreach (PuzzlePiece piece in powerablePuzzlePieces)
        {
            PoweredDoor door = piece as PoweredDoor;
            if (door != null)
            {
                door.totalLights = totalPowerSources;
            }

            PoweredChestPuzzleHandler chest = piece as PoweredChestPuzzleHandler;
            if (chest != null)
            { 
                chest.totalLights = totalPowerSources;
            }
        }
    }

    private void prepGenerator()
    {
        generator.prefabArray = new GameObject[powerablePuzzlePieces.Length];
        for (int i = 0; i < powerablePuzzlePieces.Length; i++)
        {
            generator.prefabArray[i] = powerablePuzzlePieces[i].gameObject;
        }

        totalPowerSources++;
    }

    private void prepComputers()
    {
        if (computerNetwork == null)
        {
            Debug.LogError("PuzzleAutomator: No computer network found, but computers found. Unhandled case.");
            return;
        }

        for (int i = 0; i < computers.Length; i++)
        {
            computerNetwork.computers[i] = computers[i].gameObject;
            computers[i].connectedPuzzleObject = powerablePuzzlePieces[0].gameObject;
            totalPowerSources++;
        }
    }

    private void prepPressurePlates()
    {
        foreach (PressurePlate plate in pressurePlates)
        {
            plate.objectsToPower = new GameObject[powerablePuzzlePieces.Length];
            for (int i = 0; i < powerablePuzzlePieces.Length; i++)
            {
                plate.objectsToPower[i] = powerablePuzzlePieces[i].gameObject;
            }
            totalPowerSources++;
        }
    }

    private void prepButtons()
    {
        foreach (PuzzleButtonBehavior button in puzzleButtons)
        {
            button.puzzlePieceToActivate = powerablePuzzlePieces[0];
            totalPowerSources++;
        }
    }
}
