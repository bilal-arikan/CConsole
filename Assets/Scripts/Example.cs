using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour {

    int test = 0;

	// Use this for initialization
	void Start () {

        CConsole.Log("You can log with different colours", new Color(1, 0.5f, 1));
        CConsole.Log("You can log with different colours", Color.green);
        CConsole.Log("You can log with different colours", Color.blue);
        CConsole.Log("You can log with different colours", Color.magenta);

        Debug.Log("CConsole can catches Debug.Log()'s");
        Debug.LogWarning("CConsole can catches Debug.LogWarning()'s");
        Debug.LogError("CConsole can catches Debug.LogError()'s");

        // WithArg Cmd
        CConsole.ActionsWithArg.Add("math", (s) =>
        {
            CConsole.Log(MathEvaluator.Evaluate(s));
        });
        // NoArg Cmd
        CConsole.ActionsNoArg.Add("math", () =>
        {
            Debug.Log("Example usage\"math 5+3\"");
        });
    }

    // Update is called once per frame
    void Update () {
        // throws zero division exception
        if (Input.GetKeyDown(KeyCode.Q))
            test = 5 / test;

        // throws custom exception
        if (Input.GetKeyDown(KeyCode.W))
            throw new UnityException("CConsole can catches Exceptions");
    }
}

public class MathEvaluator
{
    public static double Evaluate(string input)
    {
        String expr = "(" + input + ")";
        Stack<String> ops = new Stack<String>();
        Stack<Double> vals = new Stack<Double>();

        for (int i = 0; i < expr.Length; i++)
        {
            String s = expr.Substring(i, 1);
            if (s.Equals("(")) { }
            else if (s.Equals("+")) ops.Push(s);
            else if (s.Equals("-")) ops.Push(s);
            else if (s.Equals("*")) ops.Push(s);
            else if (s.Equals("/")) ops.Push(s);
            else if (s.Equals("sqrt")) ops.Push(s);
            else if (s.Equals(")"))
            {
                int count = ops.Count;
                while (count > 0)
                {
                    String op = ops.Pop();
                    double v = vals.Pop();
                    if (op.Equals("+")) v = vals.Pop() + v;
                    else if (op.Equals("-")) v = vals.Pop() - v;
                    else if (op.Equals("*")) v = vals.Pop() * v;
                    else if (op.Equals("/")) v = vals.Pop() / v;
                    else if (op.Equals("sqrt")) v = Math.Sqrt(v);
                    vals.Push(v);

                    count--;
                }
            }
            else vals.Push(Double.Parse(s));
        }
        return vals.Pop();
    }
}
