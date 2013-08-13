SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL';

CREATE SCHEMA IF NOT EXISTS `sourceloggingdaemon` DEFAULT CHARACTER SET latin1 COLLATE latin1_swedish_ci ;
USE `sourceloggingdaemon`;

-- -----------------------------------------------------
-- Table `sourceloggingdaemon`.`tblSources`
-- -----------------------------------------------------
CREATE  TABLE IF NOT EXISTS `sourceloggingdaemon`.`tblSources` (
  `sid` INT NOT NULL AUTO_INCREMENT ,
  `ip` VARCHAR(15) NOT NULL ,
  `port` VARCHAR(6) NOT NULL ,
  PRIMARY KEY (`sid`) )
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `sourceloggingdaemon`.`tblLogMessages`
-- -----------------------------------------------------
CREATE  TABLE IF NOT EXISTS `sourceloggingdaemon`.`tblLogMessages` (
  `lid` INT NOT NULL AUTO_INCREMENT ,
  `sid` INT NOT NULL ,
  `messageDT` DATETIME NOT NULL ,
  `messageType` VARCHAR(45) NOT NULL DEFAULT 'Generic' ,
  `userName` VARCHAR(45) NULL ,
  `userSteam` VARCHAR(45) NULL ,
  `userTeam` VARCHAR(45) NULL ,
  `targetName` VARCHAR(45) NULL ,
  `targetSteam` VARCHAR(45) NULL ,
  `targetTeam` VARCHAR(45) NULL ,
  `logLine` TEXT NOT NULL ,
  PRIMARY KEY (`lid`) ,
  INDEX `FK_LogMessages_Sources` (`sid` ASC) ,
  INDEX `Index_messageDT` USING BTREE (`messageDT` ASC) ,
  CONSTRAINT `FK_LogMessages_Sources`
    FOREIGN KEY (`sid` )
    REFERENCES `sourceloggingdaemon`.`tblSources` (`sid` )
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;



SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
