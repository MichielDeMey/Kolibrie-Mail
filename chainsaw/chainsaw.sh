#!/bin/sh
java -Dorg.apache.commons.logging.Log=org.apache.commons.logging.impl.Log4JLogger -classpath jakarta-oro-2.0.6.jar:jmdns.jar:log4j-1.3alpha-7.jar:log4j-chainsaw-2.0alpha-1.jar:log4j-optional-1.3alpha-7.jar:log4j-oro-1.3alpha-7.jar:log4j-smtp-1.3alpha-7.jar:log4j-xml-1.3alpha-7.jar:log4j-zeroconf.jar:xstream-1.1.2.jar org.apache.log4j.chainsaw.LogUI
