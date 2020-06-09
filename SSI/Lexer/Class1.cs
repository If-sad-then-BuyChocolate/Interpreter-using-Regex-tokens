using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace UDPL
{
    public class Rework
    {
        public static void Interpreter(string input)
        {
            List<string> t = GetTokens(input);
            for (int i = 0; i < t.Count; i++)
            {
                string g = t[i];                
                if (g == "var")
                {
                    string varName = Skip(2, t, ref i);
                    SkipUntil("=", t, ref i);
                    SkipSpaces(t, ref i);
                    string v = GetVariable(ReplaceVarReferences(UntilChar(";", t, ref i), '{', '}'));                    
                    if (v == "input")
                    {
                        SetVariable(varName, Console.ReadLine());
                    }
                    else
                    {
                        SetVariable(varName, v);
                    }                    
                }                
                if (g == "wait")
                {
                    string amount = GetVariable(GetNested("(", ")", t, ref i));                   
                    Thread.Sleep(int.Parse(amount));
                }
                if (g == "isdecimal")
                {
                    string varName = GetVariable(GetNested("(", ")", t, ref i));
                    string result = GetVariable(GetNested("(", ")", t, ref i));
                    decimal test = 0;
                    if (decimal.TryParse(result, out test))
                    {
                        SetVariable(varName, "true");
                    }
                    else
                    {
                        SetVariable(varName, "false");
                    }
                }
                if (g == "isnum")
                {
                    string varName = GetVariable(GetNested("(", ")", t, ref i));
                    string result = GetVariable(GetNested("(", ")", t, ref i));
                    int test = 0;
                    if (int.TryParse(result, out test))
                    {
                        SetVariable(varName, "true");
                    }
                    else
                    {
                        SetVariable(varName, "false");
                    }
                }
                if (g == "slice")
                {
                    string key = GetNested("(", ")", t, ref i);
                    string data = GetVariable(key);
                    int loc = int.Parse(GetVariable(GetNested("(", ")", t, ref i)));
                    string set = "";
                    if (loc > 0)
                    {
                        for (int x = 0; x < data.Length; x++)
                        {
                            if (x >= loc)
                            {
                                set += data[x];
                            }
                        }
                    }
                    else if(loc < 0)
                    {
                        loc = loc * -1;
                        for (int x = 0; x < data.Length; x++)
                        {
                            if (x <= loc)
                            {
                                set += data[x];
                            }
                        }
                    }
                    SetVariable(key, set);
                }
                if (g == "char")
                {
                    string key = GetNested("(", ")", t, ref i);
                    string data = GetVariable(key);
                    int loc = int.Parse(GetVariable(GetNested("(", ")", t, ref i)));
                    SetVariable(key, data[loc].ToString());
                }
                if (g == "split")
                {//split(store)(from)(split)
                    string key = GetNested("(", ")", t, ref i);
                    List<string> data = GetVariable(GetNested("(", ")", t, ref i)).Split(char.Parse(GetNested("(", ")", t, ref i))).ToList();                    
                    SetList(key, data);
                }
                if (g == "console")
                {
                    if (t[i + 1] == ";")
                    {
                        Interpreter(Console.ReadLine());
                        i++;
                    }                    
                }
                if (g == "goto")
                {
                    string File = GetVariable(GetNested("(", ")", t, ref i));
                    string line = GetVariable(GetNested("(", ")", t, ref i));
                    FromFileLine(File, line);
                }
                if (g=="clear")
                {
                    if (t[i+1] == ";")
                    {
                        Console.Clear();
                        i++;
                    }                    
                }                
                if (g == "list")
                {
                    string varName = Skip(2, t, ref i);
                    SkipUntil("=", t, ref i);
                    SkipSpaces(t, ref i);
                    string[] value = UntilChar(";", t, ref i).Split(',');
                    List<string> lst = new List<string>();
                    foreach (var item in value)
                    {
                        lst.Add(GetVariable(item));
                    }
                    SetList(varName, lst);
                }                
                if (g == "print")
                {
                    string inner = GetNested("(", ")", t, ref i);
                    string v = ReplaceVarReferences(inner, '{', '}');                   
                    string l = ReplaceListReferences(v, '[', ']');                    
                    Console.WriteLine(l);
                }
                if (g == "join")
                {                    
                    string nested = GetNested("(", ")", t, ref i);
                    string[] value = nested.Split(',');                    
                    string left = value[0];
                    string right = ReplaceListReferences(value[1], '[',']'); 
                    string name = left;
                    SetVariable(name, GetVariable(left) + GetVariable(right));
                }
                if (g == "loop")
                {
                    string amount = GetVariable(GetNested("(", ")", t, ref i));
                    string inner = GetNested("{", "}", t, ref i);
                    for (int x = 0; x < int.Parse(amount); x++)
                    {
                        Interpreter(inner);
                    }
                }
                if (g == "lstloop")
                {
                    SkipSpaces(t, ref i);
                    string ReplaceName = GetNested("(", ")", t, ref i);
                    SkipSpaces(t, ref i);
                    string ListName = GetNested("(", ")", t, ref i);
                    SkipSpaces(t, ref i);
                    string scope = GetNested("{", "}", t, ref i);
                    for (int x = 0; x < Lists[ListName].Count; x++)
                    {
                        SetVariable(ReplaceName, GetVariable(GetListIndex(ListName, x)));
                        Interpreter(scope);
                    }
                }
                if (g == "if")
                {
                    string[] param = GetParams(GetNested("(", ")", t, ref i));
                    string value = GetNested("{", "}", t, ref i);
                    IF(param[0], param[1].Trim(), param[2].Trim(), value);
                }                
                if (g == "ifelse")
                {
                    string[] param = GetParams(GetNested("(", ")", t, ref i));
                    string value1 = GetNested("{", "}", t, ref i);
                    string value2 = GetNested("{", "}", t, ref i);                   
                    IF_ELSE(param[0].Trim(), param[1].Trim(), param[2].Trim(), value1, value2);
                }                
                if (g == "while")
                {
                    string[] param = GetParams(GetNested("(", ")", t, ref i));
                    string value = GetNested("{", "}", t, ref i);
                    WHILE(param[0], param[1].Trim(), param[2].Trim(), value);
                }                
                if (g == "calc")
                {
                    string varName = GetNested("(", ")", t, ref i);
                    string problem = GetNested("{", "}", t, ref i);
                    problem = ReplaceVarReferences(problem, '{', '}');
                    string c = calc(problem);
                    SetVariable(varName, c);
                }                
                if (g == "method")
                {
                    SkipSpaces(t, ref i);
                    string VarName = GetNested("(", ")", t, ref i);
                    SkipSpaces(t, ref i);
                    string param = GetNested("(", ")", t, ref i);
                    SkipSpaces(t, ref i);
                    string scope = GetNested("{", "}", t, ref i);
                    foreach (var item in param.Split(','))
                    {
                        SetVariable(item, "unset");
                    }
                    string[] l = new string[2];
                    l[0] = param;
                    l[1] = scope;
                    SetMethod(VarName, l);
                }               
                if (g == "call")
                {
                    string varName = GetNested("(", ")", t, ref i);
                    string[] param = GetNested("(", ")", t, ref i).Split(',');
                    string[] refs = GetMethod(varName)[0].Split(',');                    
                    for (int x = 0; x < param.Count(); x++)
                    {                        
                        SetVariable(refs[x], GetVariable(param[x]));
                    }
                    Interpreter(GetMethod(varName)[1]);
                }
                if (g == "+=")
                {
                    string left = t[i - 1];
                    string right = t[i + 1];                    
                    SetVariable(left, (float.Parse(GetVariable(left)) + float.Parse(GetVariable(right))).ToString());
                }
                if (g == "-=")
                {
                    string left = t[i - 1];
                    string right = t[i + 1];
                    SetVariable(left, (float.Parse(GetVariable(left)) - float.Parse(GetVariable(right))).ToString());
                }
                if (g == "*=")
                {
                    string left = t[i - 1];
                    string right = t[i + 1];
                    SetVariable(left, (float.Parse(GetVariable(left)) * float.Parse(GetVariable(right))).ToString());
                }
                if (g == "/=")
                {
                    string left = t[i - 1];
                    string right = t[i + 1];
                    SetVariable(left, (float.Parse(GetVariable(left)) / float.Parse(GetVariable(right))).ToString());
                }                
                if (g == "++")
                {
                    float p = float.Parse(GetVariable(t[i - 1]));
                    p++;
                    SetVariable(t[i - 1], p.ToString());
                }
                if (g == "--")
                {
                    float p = float.Parse(GetVariable(t[i - 1]));
                    p--;
                    SetVariable(t[i - 1], p.ToString());
                }
                if (g == "boolswap")
                {
                    string varName = GetNested("(", ")", t, ref i);
                    string value = GetVariable(varName);
                    if (value == "true")
                    {
                        value = "false";
                    }
                    else
                    {
                        value = "true";
                    }
                    SetVariable(varName, value);
                }
                if (g == "runfile")
                {
                    string FileName = GetNested("(", ")", t, ref i);
                    FromFile(FileName+".txt");
                }
                if (g == "appendfile")
                {
                    string name = GetVariable(GetNested("(", ")", t, ref i));
                    string data = GetVariable(GetNested("{", "}", t, ref i));
                    using (StreamWriter w = File.AppendText(name+".txt"))
                    {
                        FileAppend(GetVariable(data), w);
                    }                    
                }
                if (g == "delfile")
                {
                    string name = GetVariable(GetNested("(", ")", t, ref i));
                    if (File.Exists(name+".txt"))
                    {
                        File.Delete(name + ".txt");
                    }
                }
            }
        }

        //Gathers an input from a file and runs it.
        public static void FromFile(string Filename)
        {
            string input = "";
            foreach (var item in File.ReadAllLines(Filename))
            {
                input += item.Trim();
            }
            Interpreter(input);
        }
        public static void FromFileLine(string Filename,string line)
        {
            string input = "";
            int current = 0;
            int LineSkip = int.Parse(line);
            foreach (var item in File.ReadAllLines(Filename))
            {
                if (current<LineSkip)
                {
                    continue;
                }
                input += item.Trim();
            }
            Interpreter(input);
        }
        public static void FileAppend(string logMessage, TextWriter w)
        {
            for (int i = 0; i < logMessage.Length; i++)
            {                
                if (logMessage[i] == '@')
                {                   
                    w.Write("\n");
                }               
                else
                {
                    w.Write(logMessage[i]);
                }       
            }           
        }

        //Basic Calculator (*/+-)
        public static string calc(string t)
        {
            Regex r = new Regex("([+*\\-/]|[a-zA-Z0-9]*)");
            MatchCollection mc = r.Matches(t);
            List<string> tokens = new List<string>();
            for (int i = 0; i < mc.Count; i++)
            {
                tokens.Add(mc[i].Groups[1].Value);
            }            
            for (int i = 0; i < tokens.Count(); i++)
            {
                if (tokens[i] == "*")
                {
                    tokens[i - 1] = (float.Parse(tokens[i - 1]) * float.Parse(tokens[i + 1])).ToString();
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i = 0;
                }
                else if (tokens[i] == "/")
                {

                    tokens[i - 1] = (float.Parse(tokens[i - 1]) / float.Parse(tokens[i + 1])).ToString();
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i = 0;
                }
            }
            for (int i = 0; i < tokens.Count(); i++)
            {
                if (tokens[i] == "+")
                {
                    tokens[i - 1] = (float.Parse(tokens[i - 1]) + float.Parse(tokens[i + 1])).ToString();
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i = 0;
                }
                else if (tokens[i] == "-")
                {
                    tokens[i - 1] = (float.Parse(tokens[i - 1]) - float.Parse(tokens[i + 1])).ToString();
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i = 0;
                }
            }
            return tokens[0];
        }

        //Logical analysis:
        public static void IF(string left,string op, string right, string value)
        {
            if (op == "=")
            {
                if (GetVariable(left) == GetVariable(right))
                {
                    Interpreter(value);
                }
            }
            else if (op == "!")
            {
                if (GetVariable(left) != GetVariable(right))
                {
                    Interpreter(value);
                }
            }
            else if (op == ">")
            {
                if (float.Parse(GetVariable(left)) > float.Parse(GetVariable(right)))
                {
                    Interpreter(value);
                }
            }
            else if (op == "<")
            {
                if (float.Parse(GetVariable(left)) < float.Parse(GetVariable(right)))
                {
                    Interpreter(value);
                }
            }
        }
        public static void IF_ELSE(string left,string op, string right, string value1, string value2)
        {
            if (op == "=")
            {
                if (GetVariable(left) == GetVariable(right))
                {
                    Interpreter(value1);
                }
                else
                {
                    Interpreter(value2);
                }
            }
            else if (op == "!")
            {
                if (GetVariable(left) != GetVariable(right))
                {
                    Interpreter(value1);
                }
                else
                {
                    Interpreter(value2);
                }
            }
            else if (op == ">")
            {
                if (float.Parse(GetVariable(left)) > float.Parse(GetVariable(right)))
                {
                    Interpreter(value1);
                }
                else
                {
                    Interpreter(value2);
                }
            }
            else if (op == "<")
            {
                if (float.Parse(GetVariable(left)) < float.Parse(GetVariable(right)))
                {
                    Interpreter(value1);
                }
                else
                {
                    Interpreter(value2);
                }
            }
        }
        public static void WHILE(string left, string op, string right, string value)
        {
            if (op == "=")
            {
                while (GetVariable(left) == GetVariable(right))
                {
                    Interpreter(value);
                }
            }
            else if (op == "!")
            {
                while (GetVariable(left) != GetVariable(right))
                {
                    Interpreter(value);
                }
            }
            else if (op == ">")
            {
                while (float.Parse(GetVariable(left)) > float.Parse(GetVariable(right)))
                {
                    Interpreter(value);
                }
            }
            else if (op == "<")
            {
                while (float.Parse(GetVariable(left)) < float.Parse(GetVariable(right)))
                {
                    Interpreter(value);
                }
            }
        }          
        public static string[] GetParams(string input)
        {
            string[] ret = new string[3];
            if (input.Contains("="))
            {
                string[] s = input.Split('=');
                ret[0] = s[0].Trim();
                ret[1] = "=";
                ret[2] = s[1].Trim();
            }
            else if (input.Contains("!"))
            {
                string[] s = input.Split('!');
                ret[0] = s[0].Trim();
                ret[1] = "!";
                ret[2] = s[1].Trim();
            }
            else if (input.Contains(">"))
            {
                string[] s = input.Split('>');
                ret[0] = s[0].Trim();
                ret[1] = ">";
                ret[2] = s[1].Trim();
            }
            else if (input.Contains("<"))
            {
                string[] s = input.Split('<');
                ret[0] = s[0].Trim();
                ret[1] = "<";
                ret[2] = s[1].Trim();
            }
            return ret;
        }

        //Memory allocation and retrieval.
        public static Dictionary<string, string> memory = new Dictionary<string, string>();
        public static string GetVariable(string key)
        {
            if (key == "input")
            {
                return Console.ReadLine();
            }
            string spl = key.Split(':')[0];            
            if (Lists.ContainsKey(spl))
            {
                string spl1 = key.Split(':')[1];                
                return GetListIndex(spl, int.Parse(spl1));
            }
            else if (memory.ContainsKey(key))
            {
                return memory[key];
            }           
            else
            {
                return key;
            }
        }
        public static void SetVariable(string key, string value)
        {
            if (memory.ContainsKey(key))
            {
                memory[key] = value;
            }
            else
            {
                memory.Add(key, value);
            }
        }
        public static Dictionary<string, List<string>> Lists = new Dictionary<string, List<string>>();
        public static string GetListIndex(string key, int index)
        {
            if (Lists.ContainsKey(key))
            {
                return Lists[key][index];
            }
            else
            {
                return null;
            }
        }
        public static void SetList(string key, List<string> value)
        {
            if (Lists.ContainsKey(key))
            {
                Lists[key] = value;
            }
            else
            {
                Lists.Add(key, value);
            }
        }

        //Custom Methods with paramaters and a scope:
        public static Dictionary<string, string[]> methods = new Dictionary<string,string[]>();
        public static void SetMethod(string key, string[] value)
        {
            if (methods.ContainsKey(key))
            {
                methods[key] = value;
            }
            else
            {
                methods.Add(key, value);
            }
        }
        public static string[] GetMethod(string key)
        {
            if (methods.ContainsKey(key))
            {
                return methods[key];
            }
            else
            {
                return null;
            }
        }

        //Skip or skip+Retrieve tokens.
        public static string Skip(int skip, List<string> t, ref int i)
        {
            i += skip;
            return t[i];
        }
        public static void SkipUntil(string end, List<string> t, ref int i)
        {   
            while (t[i] != end)
            {
                i++;
            }
            i++;
        }
        public static void SkipSpaces(List<string> t, ref int i)
        {            
            while (string.IsNullOrWhiteSpace(t[i]))
            {
                i++;
            }
        }

        //Returns tokens as string until it reaches a given character.
        public static string UntilChar(string chr, List<string> input, ref int i)
        {
            string ret = "";
            while (input[i] != chr)
            {
                ret += input[i];
                i++;
            }
            return ret;
        }

        //Checks if a string is a numeric value or not.
        public static bool IsNumeric(string input)
        {
            int c = 0;
            if (int.TryParse(input, out c))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Returns a sub string between a given open and close character.        
        public static string GetNested(string Open, string Close, List<string> t, ref int i)
        {
            string inner = "";
            int nested = 0;
            while (true)
            {
                if (i < t.Count - 1)
                {
                    i++;
                    if (string.IsNullOrWhiteSpace(t[i]))
                    {
                        for (int x = 0; x < t[i].Length; x++)
                        {
                            inner += " ";
                        }
                    }
                    else if (t[i] == Close)
                    {
                        nested--;
                        if (nested > 0)
                        {
                            inner += t[i];
                        }
                    }
                    else if (t[i] == Open)
                    {
                        if (nested > 0)
                        {
                            inner += t[i];
                        }
                        nested++;
                    }                    
                    else
                    {
                        inner += t[i];
                    }
                    if (nested == 0)
                    {
                        break;
                    }
                }
            }
            return inner;
        }
        
        //Replaces variable references between open and close characters with its variable value.
        public static string ReplaceVarReferences(string input,char open, char close)
        {
            string inner = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == open)
                {
                    i++;
                    string varInner = "";
                    while (input[i] != close)
                    {                        
                        varInner += input[i];
                        i++;                        
                    }
                    inner += GetVariable(varInner);
                }
                else
                {
                    inner += input[i];                    
                }
            }            
            return inner;
        }
        public static string ReplaceListReferences(string input, char open, char close)
        {
            string inner = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == open)
                {
                    i++;
                    string varInner = "";
                    while (input[i] != close)
                    {
                        varInner += input[i];
                        i++;
                    }
                    string[] s = varInner.Split(':');
                    inner += GetListIndex(s[0], int.Parse(GetVariable(s[1])));
                }
                else
                {
                    inner += input[i];
                }
            }
            return inner;
        }

        //Returns a list of tokens.
        public static List<string> GetTokens(string str)
        {
            Regex r = new Regex("(\\s+)|(\\+\\+|\\-\\-|\\+\\=|\\-\\=|\\*\\=|\\/\\=)|([,.!+\\-=\\[\\]{}()!<>?!@#$%^&*;:\"\'\\/])|([a-zA-Z0-9]*)");
            List<string> ret = new List<string>();
            foreach (Match item in r.Matches(str))
            {
                ret.Add(item.Groups[0].Value);                
            }
            return ret;
        }
    }
}
