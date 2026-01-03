terraform {
  cloud {
    organization = "ByItsCover"

    workspaces {
      name = "By_Its_Cover_Dev"
    }
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.92"
    }
  }

  required_version = ">= 1.2"
}
