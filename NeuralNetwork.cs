using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace NueralMinesweeper
{
    public class NeuralNetwork
    {
        public int[] layers;
        public float[][] neurons = Array.Empty<float[]>();
        public float[][][] weights = Array.Empty<float[][]>();
        private float fitness;

        private static readonly Random rand = new();

        int mutationC;
        double mutationS;

        public NeuralNetwork(int[] layers, int mutationC = 3, int mutationS = 100)
        {
            fitness = 0;
            this.layers = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                this.layers[i] = layers[i];

            this.mutationC = mutationC;
            this.mutationS = mutationS;

            InitNeurons();
            InitWeights();

        }

        public NeuralNetwork(NeuralNetwork copyNetwork, int mutationC = 3, int mutationS = 100)
        {
            this.mutationC = mutationC;
            this.mutationS = mutationS;

            fitness = 0;

            this.layers = new int[copyNetwork.layers.Length];
            for (int i = 0; i < copyNetwork.layers.Length; i++)
            {
                this.layers[i] = copyNetwork.layers[i];
            }

            InitNeurons();
            InitWeights();
            CopyWeights(copyNetwork.weights);

        }

        public void Import(string filePath) { weights = ImportFromCSV(filePath); }
        public void Export(string filePath) { ExportToCSV(weights, filePath); }

        static void ExportToCSV(float[][][] data, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                int depth = data.Length;

                for (int d = 0; d < depth; d++)
                {
                    int rows = data[d].Length;

                    for (int row = 0; row < rows; row++)
                    {
                        int cols = data[d][row].Length;

                        for (int col = 0; col < cols; col++)
                        {
                            writer.Write(data[d][row][col]);

                            // Add a comma if it's not the last element in the row
                            if (col < cols - 1)
                            {
                                writer.Write(",");
                            }
                        }

                        // Move to the next line if it's not the last row
                        if (row < rows - 1)
                        {
                            writer.WriteLine();
                        }
                    }

                    // Move to the next line if it's not the last depth
                    if (d < depth - 1)
                    {
                        writer.WriteLine();
                    }
                }
            }
        }

        static float[][][] ImportFromCSV(string filePath)
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);

                int depth = lines.Length;
                int rows = lines[0].Split(',').Length;

                float[][][] importedData = new float[depth][][];

                for (int d = 0; d < depth; d++)
                {
                    string[] rowValues = lines[d].Split(',');

                    importedData[d] = new float[rows][];

                    for (int row = 0; row < rows; row++)
                    {
                        string[] colValues = rowValues[row].Split(',');

                        int cols = colValues.Length; // Corrected this line

                        importedData[d][row] = new float[cols];

                        for (int col = 0; col < cols; col++)
                        {
                            if (float.TryParse(colValues[col], out float value))
                            {
                                importedData[d][row][col] = value;
                            }
                            else
                            {
                                // Handle parsing error as needed
                                Console.WriteLine($"Error parsing value at depth {d}, row {row}, col {col}");
                            }
                        }
                    }
                }

                return importedData;
            }
            return Array.Empty<float[][]>();
        }



        private void CopyWeights(float[][][] copyWeights)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        weights[i][j][k] = copyWeights[i][j][k];
                    }
                }
            }
        }

        private void InitNeurons()
        {
            List<float[]> neuronList = new List<float[]>();

            for (int i = 0; i < layers.Length; i++)
            {
                neuronList.Add(new float[layers[i]]);
            }

            neurons = neuronList.ToArray();
        }

        private void InitWeights()
        {
            Random rand = new Random();
            List<float[][]> weightList = new List<float[][]>();

            for (int i = 1; i < layers.Length; i++)
            {
                List<float[]> layerWeightList = new List<float[]>();

                int neuronsInPreviousLayer = layers[i - 1];

                for (int j = 0; j < neurons[i].Length; j++)
                {
                    float[] neuronWeights = new float[neuronsInPreviousLayer];

                    for (int k = 0; k < neuronsInPreviousLayer; k++)
                    {
                        neuronWeights[k] = (float)(rand.NextDouble() * 2 - 1);

                    }

                    layerWeightList.Add(neuronWeights);

                }

                weightList.Add(layerWeightList.ToArray());

            }

            weights = weightList.ToArray();
        }

        public float[] FeedForward(float[] inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
                //if (inputs[i] > 1 || inputs[i] < -1)
                neurons[0][i] = (float)Math.Tanh((double)inputs[i]);
            //else
            //  neurons[0][i] = inputs[i];
            for (int i = 1; i < layers.Length; i++)
            {

                for (int j = 0; j < neurons[i].Length; j++)
                {
                    float value = 0.25f;

                    for (int k = 0; k < neurons[i - 1].Length; k++)
                    {
                        value += weights[i - 1][j][k] * neurons[i - 1][k];
                    }
                    neurons[i][j] = (float)Math.Tanh(value);
                }
            }

            return neurons[neurons.Length - 1];
        }

        public void Mutate()
        {
            fitness = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        float weight = weights[i][j][k];

                        float mutationChance = (float)(rand.NextDouble() * 100);

                        if (mutationChance <= mutationC)
                        { //if 1
                          //flip sign of weight
                            weight *= -1f;
                        }
                        else if (mutationChance <= 2 * mutationC)
                        { //if 2
                          //pick random weight between -1 and 1
                            weight = (float)(rand.NextDouble() * 2 - 1);
                        }
                        else if (mutationChance <= 3 * mutationC)
                        { //if 3
                          //randomly increase by 0% to 100%
                            float factor = (float)(rand.NextDouble() + 1);
                            weight *= factor;
                        }
                        else if (mutationChance <= 4 * mutationC)
                        { //if 4
                          //randomly decrease by 0% to 100%
                            float factor = (float)rand.NextDouble();
                            weight *= factor;
                        }

                        weights[i][j][k] = weight;
                    }
                }
            }
        }


        public void Breed(NeuralNetwork parent)
        {
            fitness = 0;
            // Determine the crossover point (where to splice the weights)
            int crossoverPoint = rand.Next(weights.Length);

            for (int i = crossoverPoint; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        // Splice the weights from the parent into this network
                        weights[i][j][k] = parent.weights[i][j][k];
                    }
                }
            }
        }

        public void WoC(NeuralNetwork[] parents)
        {
            fitness = 0;

            // Determine the crossover point (where to splice the weights)

            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        float sum = weights[i][j][k];
                        foreach (NeuralNetwork n in parents)
                        {
                            sum += n.weights[i][j][k];
                        }
                        // Splice the weights from the parent into this network
                        weights[i][j][k] = (sum/parents.Count())+1;
                    }
                }
            }
        }


        public void AddFitness(float fit)
        {
            fitness += fit;
        }

        public void SetFitness(float fit)
        {
            fitness = fit;
        }

        public float GetFitness()
        {
            return fitness;
        }

        public int CompareTo(NeuralNetwork other)
        {
            if (other == null) return 1;
            if (fitness < other.fitness)
                return 1;
            else if (fitness > other.fitness)
                return -1;
            else
                return 0;
        }
    }
}
