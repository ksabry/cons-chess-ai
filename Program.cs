using System;
using Tensorflow;

namespace Chess
{
	class Program
	{
		// There is still a gc issue somewhere, gc's go up a lot after just a little bit of running, it then will run for a bit before System.AccessViolationException

		static void Main(string[] args)
		{
			var tf = new tensorflow();
			tf.compat.v1.disable_eager_execution();

			var trainer = new Trainer(tf, 123);
			trainer.TrainPairwise();
			//trainer.CompeteGenerations(18, 18);
		}
	}
}
