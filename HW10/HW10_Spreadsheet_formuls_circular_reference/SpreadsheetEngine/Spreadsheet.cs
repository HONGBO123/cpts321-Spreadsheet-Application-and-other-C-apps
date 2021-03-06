﻿using CptS321;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SpreadsheetEngine
{
    public class Spreadsheet
    {
        public UndoRedoClass UndoRedo = new UndoRedoClass();
        /*come up with a design here that actually allows the spreadsheet 
         * to create cells*/
        public event PropertyChangedEventHandler CellPropertyChanged;
        private Dictionary<string, HashSet<string>> depDict;
        public Cell[,] array;

        private class createACell : Cell
        {
            public createACell(int RowIndex, int ColumnIndex) : base(RowIndex, ColumnIndex) { }

            /*Value property is a getter only and you’ll have to implement a 
            * way to allow the spreadsheet class to set the value, but no 
            * other class can.*/
            public void SetValue(string value)
            {
                m_Value = value;
            }
        }

        /*************************************************************
        * Function: setEmpty(createACell m_Cell, Cell cell)
        * Date Created: Feb 8, 2017
        * Date Last Modified: Feb 9, 2017
        * Description: set value to empty
        * Return: void
        *************************************************************/
        private void setEmpty(createACell m_Cell, Cell cell)
        {
            m_Cell.SetValue("");
            CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));
        }

        /*************************************************************
        * Function: setExp(createACell m_Cell, Cell cell)
        * Date Created: Feb 8, 2017
        * Date Last Modified: Feb 9, 2017
        * Description: set value to exp
        * Return: void
        *************************************************************/
        private void setExp(createACell m_Cell, Cell cell, ref bool flag)
        {
            ExpTree exptree = new ExpTree(m_Cell.Text.Substring(1));
            string[] variables = exptree.GetAllVariables();

            
            foreach (string variableName in variables)
            {
                // bad reference
                if (GetCell(variableName) == null)
                {
                    m_Cell.SetValue("!(Bad Reference)");
                    CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));

                    flag = true;
                    break;
                }

                //SetExpressionVariable
                Cell variableCell = GetCell(variableName);
                double value;

                if (string.IsNullOrEmpty(variableCell.Value))
                    exptree.SetVar(variableCell.Name, 0);

                else if (!double.TryParse(variableCell.Value, out value))
                    exptree.SetVar(variableName, 0);

                else
                    exptree.SetVar(variableName, value);
                //SetExpressionVariable end;

                //self reference
                if (variableName == m_Cell.Name)
                {
                    m_Cell.SetValue("!(Self Reference)");
                    CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));
                    flag = true;
                    break;
                }
            }
            if (flag) { return; }

            //Circular reference
            foreach (string varName in variables)
            {
                if (varName == m_Cell.Name) // directly same
                {
                    m_Cell.SetValue("!(Circular Reference)");
                    CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));

                    flag = true;
                    break;
                }

                if (!depDict.ContainsKey(m_Cell.Name)) //check in dict
                {
                    continue;
                }

                string currCell = m_Cell.Name;

                for (int i = 0; i < depDict.Count; i++) //find all cell in dict
                {
                    foreach (string dependentCell in depDict[currCell]) //do same thing as before
                    {
                        if (varName == dependentCell)
                        {
                            m_Cell.SetValue("!(Circular Reference)");
                            CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));

                            flag = true;
                            break;
                        }
                        if (!depDict.ContainsKey(dependentCell))
                        {
                            continue;
                        }
                        currCell = dependentCell;
                    }
                }
            }

            if (flag) { return; }

            m_Cell.SetValue(exptree.Eval().ToString());
            CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));

            #region hw4code
            //old code from hw4
            //string letter = m_Cell.Text.Substring(1);
            //string number = letter.Substring(1);
            //string lettersAZ = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            //int col = 0, row = int.Parse(number) - 1;
            //for (int i = 0; i < 26; i++)
            //{
            //    if (lettersAZ[i] == letter[0])
            //    {
            //        col = i;
            //        break;
            //    }
            //}
            //m_Cell.SetValue(GetCell(row, col).Value.ToString());
            #endregion
        }
        
        /*************************************************************
        * Function: setExp(createACell m_Cell, Cell cell)
        * Date Created: Feb 8, 2017
        * Date Last Modified: Feb 9, 2017
        * Description: set value to exp
        * Return: void
        *************************************************************/
        private void setText(createACell m_Cell, Cell cell)
        {
            m_Cell.SetValue(m_Cell.Text);
            CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));
        }

        /*************************************************************
        * Function: Eval(Cell cell)
        * Date Created: Feb 8, 2017
        * Date Last Modified: Feb 9, 2017
        * Description: eval
        * Return: void
        *************************************************************/
        private void Eval(Cell cell)
        {
            bool flag = false;
            createACell m_Cell = cell as createACell;
            // null or empty
            if (string.IsNullOrEmpty(m_Cell.Text)) { setEmpty(m_Cell, cell); }
            // if it is an exp
            else if (m_Cell.Text[0] == '=' && m_Cell.Text.Length >= 2)
            {
                setExp(m_Cell, cell, ref flag);
                if (flag) { return; }
            }
            // others being text
            else { setText(m_Cell, cell); }

            if (depDict.ContainsKey(m_Cell.Name))
                foreach (var dependentCell in depDict[m_Cell.Name]) Eval(dependentCell);
        }
        /*************************************************************
        * Function: Eval(string exp)
        * Date Created: March 3, 2017
        * Date Last Modified: March 3, 2017
        * Description: eval
        * Return: void
        *************************************************************/
        private void Eval(string exp)
        {
            Eval(GetCell(exp));
        }

        /*************************************************************
        * Function: GetCell(string location)
        * Date Created: March 3, 2017
        * Date Last Modified: March 3, 2017
        * Description: get a cell by using a string
        * Return: Cell
        *************************************************************/
        public Cell GetCell(string exp)
        {
            char letter = exp[0];
            short number;
            Cell result;

            if (char.IsLetter(letter) == false) { return null; }

            if (short.TryParse(exp.Substring(1), out number) == false) { return null; }

            try { result = GetCell(number - 1, letter - 'A'); }
            catch { return null; }

            return result;
        }

        /*************************************************************
        * Function: Spreadsheet(int RowIndex, int ColumnIndex)
        * Date Created: Feb 8, 2017
        * Date Last Modified: Feb 9, 2017
        * Description: Spreadsheet cons
        * Return: NONE
        *************************************************************/
        public Spreadsheet(int RowIndex, int ColumnIndex)
        {
            array = new Cell[RowIndex, ColumnIndex];

            depDict = new Dictionary<string, HashSet<string>>();

            for (int x = 0; x < RowIndex; x++)
            {
                for (int y = 0; y < ColumnIndex; y++)
                {
                    array[x, y] = new createACell(x, y);
                    array[x, y].PropertyChanged += OnPropertyChanged;
                }
            }
        }

        /*************************************************************
        * Function: OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        * Date Created: Feb 8, 2017
        * Date Last Modified: Feb 9, 2017
        * Description: OnPropertyChanged
        * Return: void
        *************************************************************/
        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                createACell tmpCell = sender as createACell;
                RemoveDep(tmpCell.Name);

                if (tmpCell.Text != "" && tmpCell.Text[0] == '=')
                {
                    ExpTree exp = new ExpTree(tmpCell.Text.Substring(1));
                    MakeDep(tmpCell.Name, exp.GetAllVariables());
                }
                Eval(sender as Cell);
            }
            else if (e.PropertyName == "BGColor")
            {
                CellPropertyChanged(sender, new PropertyChangedEventArgs("BGColor"));
            }
        }

        /*************************************************************
        * Function: RemoveDep(string cellName)
        * Date Created: March 3, 2017
        * Date Last Modified: March 3, 2017
        * Description: Removes all dependencies from the cell name
        * Return: void
        *************************************************************/
        private void RemoveDep(string cellName)
        {
            List<string> dependenciesList = new List<string>();

            foreach (string key in depDict.Keys)
            {
                if (depDict[key].Contains(cellName))
                    dependenciesList.Add(key);
            }

            foreach (string key in dependenciesList)
            {
                HashSet<string> hashset = depDict[key];
                if (hashset.Contains(cellName))
                    hashset.Remove(cellName);
            }
        }

        /*************************************************************
        * Function: MakeDep(string cellName, string[] variablesUsed)
        * Date Created: March 3, 2017
        * Date Last Modified: March 3, 2017
        * Description: Removes all dependencies from the cell name
        * Return: void
        *************************************************************/
        private void MakeDep(string cellName, string[] variablesUsed)
        {
            for (int i = 0; i < variablesUsed.Length; i++)
            {
                if (depDict.ContainsKey(variablesUsed[i]) == false)
                {
                    depDict[variablesUsed[i]] = new HashSet<string>();
                }
                depDict[variablesUsed[i]].Add(cellName);
            }
        }

        /*************************************************************
        * Function: GetCell(int RowIndex, int ColumnIndex)
        * Date Created: March 3, 2017
        * Date Last Modified: March 3, 2017
        * Description: get cell from given row and col
        * Return: Cell
        *************************************************************/
        public Cell GetCell(int RowIndex, int ColumnIndex)
        {
            return array[RowIndex, ColumnIndex];
        }

        //getter
        public int RowCount { get { return array.GetLength(0); } }
        public int ColumnCount { get { return array.GetLength(1); } }

        /*************************************************************
        * Function: Save(Stream outfile)
        * Date Created: March 20, 2017
        * Date Last Modified: March 20, 2017
        * Description: save file
        * Return: NONE
        *************************************************************/
        public void Save(Stream outfile)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter writeXml = XmlWriter.Create(outfile, settings);

            writeXml.WriteStartElement("spreadsheet");

            foreach (Cell m_cell in array)
            {
                if (m_cell.Text != "" || m_cell.Value != "" || m_cell.BGColor != 4294967295)
                {
                    writeXml.WriteStartElement("cell");
                    writeXml.WriteAttributeString("name", m_cell.Name);
                    writeXml.WriteElementString("bgcolor", m_cell.BGColor.ToString("x8"));
                    writeXml.WriteElementString("text", m_cell.Text.ToString());
                    writeXml.WriteEndElement();
                }
            }
            writeXml.WriteEndElement();
            writeXml.Close();
        }

        /*************************************************************
        * Function: Load(Stream outfile)
        * Date Created: March 20, 2017
        * Date Last Modified: March 20, 2017
        * Description: load file
        * Return: NONE
        *************************************************************/
        public void Load(Stream infile)
        {
            XDocument inf = XDocument.Load(infile);

            foreach (XElement label in inf.Root.Elements("cell"))
            {
                Cell m_cell = GetCell(label.Attribute("name").Value);

                if (label.Element("text") != null)
                {
                    m_cell.Text = label.Element("text").Value.ToString();
                }
                if (label.Element("bgcolor") != null)
                {
                    m_cell.BGColor = uint.Parse(label.Element("bgcolor").Value, System.Globalization.NumberStyles.HexNumber);  // Set cell background color to the tag's backgroundcolor element value
                }
            }
        }
    }
}
