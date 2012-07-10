﻿using System;
using NUnit.Framework;
using Rhino.Mocks;
using StatsdClient;

namespace Tests
{
    [TestFixture]
    public class UnitTests
    {
        private IStatsdUDP udp;
        private IRandomGenerator _randomGenerator;
        [SetUp]
        public void Setup()
        {
            udp = MockRepository.GenerateMock<IStatsdUDP>();
            _randomGenerator = MockRepository.GenerateMock<IRandomGenerator>();
            _randomGenerator.Stub(x => x.ShouldSend(Arg<double>.Is.Anything)).Return(true);
        }
   
        [Test]
        public void Increases_counter_with_value_of_X()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Send<Statsd.Counting>("counter", 5);
            udp.AssertWasCalled(x => x.Send("counter:5|c"));
        }

        [Test]
        public void Adds_timing()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Send<Statsd.Timing>("timer", 5);
            udp.AssertWasCalled(x => x.Send("timer:5|ms"));
        }

        [Test]
        public void Increases_counter_with_value_of_X_and_sample_rate()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Send<Statsd.Counting>("counter", 5,0.1);
            udp.AssertWasCalled(x => x.Send("counter:5|c|@0.1"));
        }

        [Test]
        public void Add_timing_with_sample_rate()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Send<Statsd.Timing>("timer", 5,0.1);
            udp.AssertWasCalled(x => x.Send("timer:5|ms|@0.1"));
        }


        [Test]
        public void Adds_gauge()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Send<Statsd.Gauge>("gauge", 5);
            udp.AssertWasCalled(x => x.Send("gauge:5|g"));
        }

        [Test]
        public void Add_gauge_with_sample_rate()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Send<Statsd.Gauge>("gauge", 5, 0.1);
            udp.AssertWasCalled(x => x.Send("gauge:5|g|@0.1"));
        }

        [Test]
        public void counting_exception_fails_silently()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
            s.Send<Statsd.Counting>("counter", 5);
            Assert.Pass();
        }

        [Test]
        public void timing_exception_fails_silently()
        {
            udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
            Statsd s = new Statsd(udp);
            s.Send<Statsd.Timing>("timer", 5);
            Assert.Pass();
        }

        [Test]
        public void gauge_exception_fails_silently()
        {
            udp.Stub(x=>x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
            Statsd s = new Statsd(udp);
            s.Send<Statsd.Gauge>("gauge", 5);
            Assert.Pass();
        }

        [Test]
        public void add_one_counter_and_one_gauge_shows_in_commands()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Add<Statsd.Counting>("counter",1,0.1);
            s.Add<Statsd.Timing>("timer", 1, 0.1);       
    
            Assert.That(s.Commands.Count,Is.EqualTo(2));
            Assert.That(s.Commands[0],Is.EqualTo("counter:1|c|@0.1"));
            Assert.That(s.Commands[1], Is.EqualTo("timer:1|ms|@0.1"));
        }

        [Test]
        public void add_one_counter_and_one_gauge_sends_in_one_go()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Add<Statsd.Counting>("counter", 1, 0.1);
            s.Add<Statsd.Timing>("timer", 1, 0.1);
            s.Send();

            udp.AssertWasCalled(x => x.Send("counter:1|c|@0.1" + Environment.NewLine + "timer:1|ms|@0.1"));
        }


        [Test]
        public void add_one_counter_and_one_gauge_sends_and_removes_commands()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Add<Statsd.Counting>("counter", 1, 0.1);
            s.Add<Statsd.Timing>("timer", 1, 0.1);
            s.Send();

            Assert.That(s.Commands.Count,Is.EqualTo(0));
        }

        [Test]
        public void add_one_counter_and_send_one_gauge_sends_only_sends_the_last()
        {
            Statsd s = new Statsd(udp, _randomGenerator);
            s.Add<Statsd.Counting>("counter", 1, 0.1);
            s.Send<Statsd.Timing>("timer", 1, 0.1);

            udp.AssertWasCalled(x => x.Send("timer:1|ms|@0.1"));
        }
    }
}
