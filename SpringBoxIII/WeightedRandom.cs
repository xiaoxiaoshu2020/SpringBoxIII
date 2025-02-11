using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpringBoxIII
{
    class WeightedRandom
    {
        private List<int> values;
        private List<int> weights;
        private int totalWeight;

        // 构造函数，传入元素的值及对应的权重
        public WeightedRandom(List<int> values, List<int> weights)
        {
            if (values.Count != weights.Count)
                throw new ArgumentException("Values and weights must have the same length.");

            this.values = values;
            this.weights = weights;
            totalWeight = 0;

            // 计算所有权重的总和
            foreach (var weight in weights)
            {
                totalWeight += weight;
            }
        }

        // 获取加权随机元素
        public int GetRandomValue()
        {
            Random random = new Random();
            int randomWeight = random.Next(totalWeight);

            int accumulatedWeight = 0;
            for (int i = 0; i < values.Count; i++)
            {
                accumulatedWeight += weights[i];
                if (randomWeight < accumulatedWeight)
                {
                    return values[i];
                }
            }
            return -1;
        }
    }
}
