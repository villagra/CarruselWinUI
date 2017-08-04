using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carrusel.Controls
{
    public class CircularList<T>
    {
        List<Tuple<int, T>> _items;
        int _selected = 0;

        public List<Tuple<int, T>> Items { get { return _items; } }
        public int SelectedIndex { get { return _selected; } }

        public CircularList()
        {
            _items = new List<Tuple<int, T>>();
        }

        public void AddLeft(T newItem)
        {
            int idx = 0;
            if (_items.Count > 0)
            {
                idx = _items.First().Item1 - 1;
            }

            _items.Insert(0, new Tuple<int, T>(idx, newItem));
            PrintAll();
        }

        public void AddRight(T newItem)
        {
            int idx = 0;
            if (_items.Count > 0)
            {
                idx = _items.Last().Item1 + 1;
            }

            _items.Add(new Tuple<int, T>(idx, newItem));
            PrintAll();
        }

        public Tuple<int, T> MoveLeft()
        {
            var last = _items.Last();            
            _items.Remove(last);
            _items.Insert(0, new Tuple<int, T>(_items.First().Item1 - 1, last.Item2));
            _selected--;

            PrintAll();
            return _items.First();
        }

        /// <summary>
        /// returns the new position in collection, and the item moved
        /// </summary>
        /// <returns></returns>
        public Tuple<int, T> MoveRight()
        {
            var first = _items.First();
            _items.Remove(first);
            _items.Add(new Tuple<int, T>(_items.Last().Item1 + 1, first.Item2));
            _selected++;

            PrintAll();
            return _items.Last();
        }

        private void PrintAll()
        {
            Debug.WriteLine("Start collection dump");
            Debug.WriteLine($"Selected: {_selected}");
            foreach (var item in _items)
            {
                Debug.WriteLine(item.Item1.ToString());
            }
            Debug.WriteLine("End collection dump");
        }
    }
}
